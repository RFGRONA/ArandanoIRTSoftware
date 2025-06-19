using Supabase;
// Asegúrate que tus modelos (SensorDataModel, DeviceDataModel, etc.) estén en este namespace
using System.Text.Json;
using Supabase.Postgrest.Models; // Para BaseModel
using Supabase.Postgrest.Responses; // Para CountResponse si fuera necesario (aunque .Count() devuelve Task<long>)
using System.Reflection;
using ArandanoIRT.Web.Common;
using ArandanoIRT.Web.Data.DTOs.Admin;
using ArandanoIRT.Web.Data.Models;
using ArandanoIRT.Web.Services.Contracts; // Para GetProperty

namespace ArandanoIRT.Web.Services.Implementation;

public class DataQueryService : IDataQueryService
{
    private readonly Client _supabaseClient;
    private readonly ILogger<DataQueryService> _logger;

    private const string StatusNameActive = "ACTIVE";

    public DataQueryService(Client supabaseClient, ILogger<DataQueryService> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    // Asumiendo que BaseModel tiene una propiedad Id de tipo int o long.
    // Si tus modelos de Supabase no heredan de un BaseModel común con 'Id', ajusta esto.
    // Para este ejemplo, se asume que BaseModel tiene 'Id' y los modelos como DeviceDataModel tienen 'Name'.
    private Supabase.Interfaces.ISupabaseTable<T, Supabase.Realtime.RealtimeChannel> GetTable<T>()
        where T : BaseModel, new() => _supabaseClient.From<T>();

    private async Task<Dictionary<int, string>> GetNamesMapAsync<TModel>(List<int> ids, string nameColumn = "Name") // Cambiado "name" a "Name" para seguir convenciones C#
        where TModel : BaseModel, new()
    {
        var map = new Dictionary<int, string>();
        if (ids == null || !ids.Any()) return map;

        try
        {
            // Asegurarse que la propiedad 'Id' exista y sea accesible.
            // La propiedad 'Id' usualmente viene de 'BaseModel' o es una propiedad directa.
            // El nombre de columna en la BD puede ser 'id' pero la propiedad en C# puede ser 'Id'.
            // Supabase client maneja el mapeo si usas atributos [Column("column_name")].
            var selectColumns = $"id,{nameColumn.ToLower()}"; // Supabase a menudo prefiere nombres de columna en minúscula para el select explícito
            if (nameColumn.Equals("Id", StringComparison.OrdinalIgnoreCase)) // Si se pide el mismo Id como "name"
            {
                selectColumns = "id";
            }


            var response = await GetTable<TModel>()
                .Select(selectColumns) // Selecciona 'id' y la columna de nombre (ej. 'name')
                .Filter("id", Supabase.Postgrest.Constants.Operator.In, ids)
                .Get();

            if (response?.Models != null)
            {
                foreach (var model in response.Models)
                {
                    // Usar reflexión para obtener Id y la propiedad de nombre dinámicamente
                    PropertyInfo? idPropInfo = typeof(TModel).GetProperty("Id", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    PropertyInfo? namePropInfo = typeof(TModel).GetProperty(nameColumn, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                    if (idPropInfo != null && namePropInfo != null)
                    {
                        var idValObj = idPropInfo.GetValue(model);
                        var nameValObj = namePropInfo.GetValue(model);

                        // Confirmado que todos los IDs son INT
                        if (idValObj is int idVal && nameValObj is string nameVal)
                        {
                            map[idVal] = nameVal;
                        }
                        // Si el Id fuera long, sería: else if (idValObj is long idValLong && nameValObj is string nameValStr) { map[(int)idValLong] = nameValStr; }
                        else
                        {
                            _logger.LogWarning("No se pudo convertir Id ({IdType}) o Name ({NameType}) para el modelo {ModelType} con Id: {IdValue}",
                                idValObj?.GetType().Name ?? "null", nameValObj?.GetType().Name ?? "null", typeof(TModel).FullName, idValObj?.ToString() ?? "N/A");
                        }
                    }
                    else
                    {
                        if (idPropInfo == null) _logger.LogWarning("No se pudo encontrar la propiedad 'Id' en el tipo {ModelType} usando reflexión.", typeof(TModel).FullName);
                        if (namePropInfo == null) _logger.LogWarning("No se pudo encontrar la propiedad de nombre '{NameColumn}' en el tipo {ModelType} usando reflexión.", nameColumn, typeof(TModel).FullName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo mapa de nombres para {ModelName} usando columna '{NameColumn}'", typeof(TModel).Name, nameColumn);
        }
        return map;
    }

    private async Task<Result<int>> GetStatusIdAsync(string statusName)
    {
        try
        {
            var response = await GetTable<StatusModel>() // Asumiendo que StatusModel tiene Id (int) y Name (string)
                .Filter("name", Supabase.Postgrest.Constants.Operator.Equals, statusName) // Columna 'name' en la tabla 'statuses'
                .Single();

            if (response == null)
            {
                _logger.LogError("Estado '{StatusName}' no encontrado en la base de datos.", statusName);
                return Result.Failure<int>($"Estado '{statusName}' no configurado.");
            }
            return Result.Success(response.Id); // Asumiendo response.Id es int
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el ID del estado '{StatusName}'.", statusName);
            return Result.Failure<int>($"Error interno al buscar estado: {ex.Message}");
        }
    }

    public async Task<Result<PagedResultDto<SensorDataDisplayDto>>> GetSensorDataAsync(DataQueryFilters filters)
    {
        _logger.LogInformation("Obteniendo datos de sensores con filtros: {@Filters}", JsonSerializer.Serialize(filters));
        try
        {
            // Función para construir la consulta base
            Func<Supabase.Postgrest.Interfaces.IPostgrestTable<SensorDataModel>> baseQueryBuilder = () => 
            {
                var q = _supabaseClient.From<SensorDataModel>() // Usar _supabaseClient.From en lugar de GetTable si SensorDataModel no hereda de BaseModel que espera GetTable
                    .Order("recorded_at", Supabase.Postgrest.Constants.Ordering.Descending);

                if (filters.DeviceId.HasValue && filters.DeviceId > 0)
                    q = q.Filter("device_id", Supabase.Postgrest.Constants.Operator.Equals, filters.DeviceId.Value);
                if (filters.PlantId.HasValue && filters.PlantId > 0)
                    q = q.Filter("plant_id", Supabase.Postgrest.Constants.Operator.Equals, filters.PlantId.Value);
                if (filters.CropId.HasValue && filters.CropId > 0)
                    q = q.Filter("crop_id", Supabase.Postgrest.Constants.Operator.Equals, filters.CropId.Value);

                TimeZoneInfo? colombiaZone = null;
            try
            {
                colombiaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            }
            catch (TimeZoneNotFoundException tzex)
            {
                _logger.LogError(tzex, "Zona horaria 'America/Bogota' no encontrada. Los filtros de fecha pueden no ser precisos.");
                // Si no se encuentra, las conversiones explícitas fallarán.
                // ToUniversalTime() para Kind.Local aún funcionaría basado en la zona del servidor.
                // Kind.Unspecified tratado con ToUniversalTime() también se basaría en la zona del servidor.
            }

            if (filters.StartDate.HasValue)
            {
                DateTime filterDate = filters.StartDate.Value;
                DateTime startDateUtc;

                if (filterDate.Kind == DateTimeKind.Unspecified && colombiaZone != null)
                {
                    // Asumir que la fecha sin especificar del filtro es hora de Colombia y convertirla a UTC.
                    startDateUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(filterDate, DateTimeKind.Unspecified), colombiaZone);
                    _logger.LogDebug("Filtro StartDate (Unspecified) '{FilterDate}' interpretado como America/Bogota y convertido a UTC '{StartDateUtc}'.", filterDate, startDateUtc);
                }
                else if (filterDate.Kind == DateTimeKind.Local)
                {
                    startDateUtc = filterDate.ToUniversalTime();
                    _logger.LogDebug("Filtro StartDate (Local) '{FilterDate}' convertido a UTC '{StartDateUtc}' usando la zona del servidor.", filterDate, startDateUtc);
                }
                else // Ya es Utc o no se pudo convertir explícitamente por zona no encontrada (se usará como viene o ToUniversalTime si es Local)
                {
                    startDateUtc = (filterDate.Kind == DateTimeKind.Unspecified) ? filterDate.ToUniversalTime() : filterDate; // Fallback para Unspecified si colombiaZone es null
                    if(filterDate.Kind == DateTimeKind.Unspecified && colombiaZone == null) 
                         _logger.LogDebug("Filtro StartDate (Unspecified) '{FilterDate}' convertido a UTC '{StartDateUtc}' usando ToUniversalTime() (fallback por zona America/Bogota no encontrada).", filterDate, startDateUtc);
                    else
                         _logger.LogDebug("Filtro StartDate ya es UTC o se usa como viene: '{StartDateUtc}'.", startDateUtc);
                }
                q = q.Filter("recorded_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, startDateUtc.ToString("o"));
            }

            if (filters.EndDate.HasValue)
            {
                DateTime filterDate = filters.EndDate.Value;
                DateTime endDateUtc;

                // Queremos que EndDate incluya todo el día seleccionado en hora Colombia.
                // Tomamos la parte de la fecha, establecemos la hora al final del día, luego convertimos esa hora Colombia a UTC.
                DateTime endOfDayInFilterDate = new DateTime(filterDate.Year, filterDate.Month, filterDate.Day, 23, 59, 59, 999);

                if (filterDate.Kind == DateTimeKind.Unspecified && colombiaZone != null)
                {
                    // Asumir que 'endOfDayInFilterDate' (derivado de una entrada Unspecified) es hora de Colombia.
                    endDateUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(endOfDayInFilterDate, DateTimeKind.Unspecified), colombiaZone);
                     _logger.LogDebug("Filtro EndDate (Unspecified) '{FilterDate}' (fin de día Colombia '{EndOfDayInFilterDate}') convertido a UTC '{EndDateUtc}'.", filterDate, endOfDayInFilterDate, endDateUtc);
                }
                else if (filterDate.Kind == DateTimeKind.Local)
                {
                    // 'endOfDayInFilterDate' tomará la zona local del servidor, luego la convertimos a UTC.
                    endDateUtc = DateTime.SpecifyKind(endOfDayInFilterDate, DateTimeKind.Local).ToUniversalTime();
                    _logger.LogDebug("Filtro EndDate (Local) '{FilterDate}' (fin de día local '{EndOfDayInFilterDate}') convertido a UTC '{EndDateUtc}'.", filterDate, endOfDayInFilterDate, endDateUtc);
                }
                else // Ya es Utc, o Unspecified y no se encontró la zona Colombia (fallback)
                {
                     // Si ya es UTC, el usuario especificó un momento UTC. Si era Unspecified y no hay zona,
                     // ToUniversalTime() en endOfDayInFilterDate (que es Unspecified) lo tratará como Local del servidor.
                    endDateUtc = (filterDate.Kind == DateTimeKind.Unspecified) ? DateTime.SpecifyKind(endOfDayInFilterDate, DateTimeKind.Unspecified).ToUniversalTime() : DateTime.SpecifyKind(endOfDayInFilterDate, DateTimeKind.Utc);
                    if(filterDate.Kind == DateTimeKind.Unspecified && colombiaZone == null)
                        _logger.LogDebug("Filtro EndDate (Unspecified) '{FilterDate}' (fin de día '{EndOfDayInFilterDate}') convertido a UTC '{EndDateUtc}' usando ToUniversalTime() (fallback).", filterDate, endOfDayInFilterDate, endDateUtc);
                    else if (filterDate.Kind == DateTimeKind.Utc)
                         _logger.LogDebug("Filtro EndDate ya es UTC: '{FilterOriginalDate}'. Se usará el fin de este día UTC: {EndDateUtc}", filterDate, endDateUtc);
                    else // Unspecified pero colombiaZone fue encontrado y usado arriba, este caso no debería darse si Kind es Utc.
                         _logger.LogDebug("Filtro EndDate (UTC o procesado de otra forma): '{FilterOriginalDate}'. Fin de día calculado en UTC: {EndDateUtc}", filterDate, endDateUtc);
                }
                q = q.Filter("recorded_at", Supabase.Postgrest.Constants.Operator.LessThanOrEqual, endDateUtc.ToString("o"));
            }
            return q;
        };

            var countQuery = baseQueryBuilder();
            long totalCount = await countQuery.Count(Supabase.Postgrest.Constants.CountType.Exact);
            _logger.LogInformation("Conteo total de SensorData para filtros {@Filters}: {TotalCount}", JsonSerializer.Serialize(filters), totalCount);

            var displayDtos = new List<SensorDataDisplayDto>();
            if (totalCount > 0)
            {
                var dataQuery = baseQueryBuilder(); // Reconstruir la consulta para obtener los datos
                var (from, to) = ((filters.PageNumber - 1) * filters.PageSize, filters.PageNumber * filters.PageSize - 1);
                var response = await dataQuery.Range(from, to).Get();
                _logger.LogInformation("SensorData recuperados de Supabase para página {PageNumber} (tamaño {PageSize}): {Count}", filters.PageNumber, filters.PageSize, response?.Models?.Count ?? 0);

                if (response?.Models != null && response.Models.Any())
                {
                    var deviceIds = response.Models.Select(m => m.DeviceId).Distinct().ToList();
                    var plantIds = response.Models.Where(m => m.PlantId.HasValue).Select(m => m.PlantId!.Value).Distinct().ToList();
                    var cropIds = response.Models.Where(m => m.CropId.HasValue).Select(m => m.CropId!.Value).Distinct().ToList();

                    // Asumiendo que DeviceDataModel, PlantDataModel, CropModel tienen una propiedad "Name"
                    var deviceNamesTask = GetNamesMapAsync<DeviceDataModel>(deviceIds, "Name");
                    var plantNamesTask = GetNamesMapAsync<PlantDataModel>(plantIds, "Name");
                    var cropNamesTask = GetNamesMapAsync<CropModel>(cropIds, "Name");

                    await Task.WhenAll(deviceNamesTask, plantNamesTask, cropNamesTask);

                    var deviceNames = deviceNamesTask.Result;
                    var plantNames = plantNamesTask.Result;
                    var cropNames = cropNamesTask.Result;

                    displayDtos = response.Models.Select(m =>
                    {
                        return new SensorDataDisplayDto
                        {
                            Id = m.Id,
                            DeviceId = m.DeviceId,
                            DeviceName = deviceNames.TryGetValue(m.DeviceId, out var dn) ? dn : m.DeviceId.ToString(),
                            PlantName = m.PlantId.HasValue && plantNames.TryGetValue(m.PlantId.Value, out var pn) ? pn : "N/A",
                            CropName = m.CropId.HasValue && cropNames.TryGetValue(m.CropId.Value, out var cn) ? cn : "N/A",
                            Light = m.Light,
                            Temperature = m.Temperature,
                            Humidity = m.Humidity,
                            CityTemperature = m.CityTemperature,
                            CityHumidity = m.CityHumidity,
                            CityWeatherCondition = m.CityWeatherCondition,
                            IsNight = m.IsNight,
                            RecordedAt = m.RecordedAt.ToColombiaTime()
                        };
                    }).ToList();
                }
            } else {
                 _logger.LogInformation("No se encontraron SensorData para los filtros aplicados o el conteo fue cero.");
            }


            return Result.Success(new PagedResultDto<SensorDataDisplayDto>
            {
                Items = displayDtos,
                PageNumber = filters.PageNumber,
                PageSize = filters.PageSize,
                TotalCount = (int)totalCount // PagedResultDto.TotalCount es int
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo datos de sensores con filtros {@Filters}.", JsonSerializer.Serialize(filters));
            return Result.Failure<PagedResultDto<SensorDataDisplayDto>>($"Error interno al obtener datos de sensores: {ex.Message}");
        }
    }

    public async Task<Result<PagedResultDto<ThermalCaptureSummaryDto>>> GetThermalCapturesAsync(DataQueryFilters filters)
    {
        _logger.LogInformation("Obteniendo capturas térmicas con filtros: {@Filters}", JsonSerializer.Serialize(filters));
        try
        {
            Func<Supabase.Postgrest.Interfaces.IPostgrestTable<ThermalDataModel>> baseQueryBuilder = () =>
            {
                var q = _supabaseClient.From<ThermalDataModel>()
                    .Order("recorded_at", Supabase.Postgrest.Constants.Ordering.Descending);

                if (filters.DeviceId.HasValue && filters.DeviceId > 0)
                    q = q.Filter("device_id", Supabase.Postgrest.Constants.Operator.Equals, filters.DeviceId.Value);
                if (filters.PlantId.HasValue && filters.PlantId > 0)
                    q = q.Filter("plant_id", Supabase.Postgrest.Constants.Operator.Equals, filters.PlantId.Value);
                if (filters.CropId.HasValue && filters.CropId > 0)
                     q = q.Filter("crop_id", Supabase.Postgrest.Constants.Operator.Equals, filters.CropId.Value);

                TimeZoneInfo? colombiaZone = null;
                try { colombiaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota"); }
                catch (TimeZoneNotFoundException tzex) { _logger.LogError(tzex, "Zona 'America/Bogota' no encontrada para filtros en GetThermalCapturesAsync."); }

                if (filters.StartDate.HasValue)
                {
                    DateTime filterDate = filters.StartDate.Value;
                    DateTime startDateUtc;
                    if (filterDate.Kind == DateTimeKind.Unspecified && colombiaZone != null) {
                        startDateUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(filterDate, DateTimeKind.Unspecified), colombiaZone);
                    } else if (filterDate.Kind == DateTimeKind.Local) {
                        startDateUtc = filterDate.ToUniversalTime();
                    } else {
                        startDateUtc = (filterDate.Kind == DateTimeKind.Unspecified) ? filterDate.ToUniversalTime() : filterDate;
                    }
                    q = q.Filter("recorded_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, startDateUtc.ToString("o"));
                }
                if (filters.EndDate.HasValue)
                {
                    DateTime filterDate = filters.EndDate.Value;
                    DateTime endDateUtc;
                    DateTime endOfDayInFilterDate = new DateTime(filterDate.Year, filterDate.Month, filterDate.Day, 23, 59, 59, 999);
                    if (filterDate.Kind == DateTimeKind.Unspecified && colombiaZone != null) {
                        endDateUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(endOfDayInFilterDate, DateTimeKind.Unspecified), colombiaZone);
                    } else if (filterDate.Kind == DateTimeKind.Local) {
                        endDateUtc = DateTime.SpecifyKind(endOfDayInFilterDate, DateTimeKind.Local).ToUniversalTime();
                    } else {
                         endDateUtc = (filterDate.Kind == DateTimeKind.Unspecified) ? DateTime.SpecifyKind(endOfDayInFilterDate, DateTimeKind.Unspecified).ToUniversalTime() : DateTime.SpecifyKind(endOfDayInFilterDate, DateTimeKind.Utc);
                    }
                    q = q.Filter("recorded_at", Supabase.Postgrest.Constants.Operator.LessThanOrEqual, endDateUtc.ToString("o"));
                }
                return q;
            };

            var countQuery = baseQueryBuilder();
            long totalCount = await countQuery.Count(Supabase.Postgrest.Constants.CountType.Exact);
            _logger.LogInformation("Conteo total de ThermalData para filtros {@Filters}: {TotalCount}", JsonSerializer.Serialize(filters), totalCount);
            
            var summaries = new List<ThermalCaptureSummaryDto>();
            if (totalCount > 0) {
                var dataQuery = baseQueryBuilder();
                var (from, to) = ((filters.PageNumber - 1) * filters.PageSize, filters.PageNumber * filters.PageSize - 1);
                var response = await dataQuery.Range(from, to).Get();
                _logger.LogInformation("ThermalData recuperados de Supabase para página {PageNumber} (tamaño {PageSize}): {Count}", filters.PageNumber, filters.PageSize, response?.Models?.Count ?? 0);

                if (response?.Models != null && response.Models.Any())
                {
                    var deviceIds = response.Models.Select(m => m.DeviceId).Distinct().ToList();
                    var plantIds = response.Models.Where(m => m.PlantId.HasValue).Select(m => m.PlantId!.Value).Distinct().ToList();

                    var deviceNamesTask = GetNamesMapAsync<DeviceDataModel>(deviceIds, "Name");
                    var plantNamesTask = GetNamesMapAsync<PlantDataModel>(plantIds, "Name");
                    await Task.WhenAll(deviceNamesTask, plantNamesTask);
                    var deviceNames = deviceNamesTask.Result;
                    var plantNames = plantNamesTask.Result;

                    foreach (var m in response.Models)
                    {
                        Data.DTOs.DeviceApi.ThermalDataDto? thermalStats = null;
                        if (!string.IsNullOrEmpty(m.ThermalImageData))
                        {
                            try
                            {
                                thermalStats = JsonSerializer.Deserialize<Data.DTOs.DeviceApi.ThermalDataDto>(m.ThermalImageData,
                                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            }
                            catch (JsonException jsx)
                            {
                                _logger.LogWarning(jsx, "No se pudo deserializar ThermalImageData para ID {ThermalDataId} en GetThermalCapturesAsync", m.Id);
                            }
                        }

                        summaries.Add(new ThermalCaptureSummaryDto
                        {
                            Id = m.Id,
                            DeviceId = m.DeviceId,
                            DeviceName = deviceNames.TryGetValue(m.DeviceId, out var dn) ? dn : m.DeviceId.ToString(),
                            PlantName = m.PlantId.HasValue && plantNames.TryGetValue(m.PlantId.Value, out var pn) ? pn : "N/A",
                            MaxTemp = thermalStats?.Max_Temp ?? 0,
                            MinTemp = thermalStats?.Min_Temp ?? 0,
                            AvgTemp = thermalStats?.Avg_Temp ?? 0,
                            RgbImagePath = m.RgbImagePath,
                            RecordedAt = m.RecordedAt.ToColombiaTime()
                        });
                    }
                }
            } else {
                _logger.LogInformation("No se encontraron ThermalData para los filtros aplicados o el conteo fue cero.");
            }

            return Result.Success(new PagedResultDto<ThermalCaptureSummaryDto>
            {
                Items = summaries,
                PageNumber = filters.PageNumber,
                PageSize = filters.PageSize,
                TotalCount = (int)totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo capturas térmicas con filtros {@Filters}.", JsonSerializer.Serialize(filters));
            return Result.Failure<PagedResultDto<ThermalCaptureSummaryDto>>($"Error interno al obtener capturas térmicas: {ex.Message}");
        }
    }

    public async Task<Result<ThermalCaptureDetailsDto?>> GetThermalCaptureDetailsAsync(long captureId)
{
    _logger.LogInformation("Obteniendo detalles de captura térmica ID: {CaptureId}", captureId);
    try
    {
        var capture = await _supabaseClient.From<ThermalDataModel>()
            .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, captureId.ToString()) // Convertir captureId (long) a string
            .Single();

        if (capture == null)
        {
            _logger.LogWarning("No se encontró captura térmica con ID: {CaptureId}", captureId);
            return Result.Success<ThermalCaptureDetailsDto?>(null);
        }

        // El resto del método para obtener Device, Plant, Crop...
        DeviceDataModel? device = await _supabaseClient.From<DeviceDataModel>()
            .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, capture.DeviceId.ToString()) // También convertir DeviceId (int) a string por consistencia
            .Single();

        PlantDataModel? plant = null;
        CropModel? crop = null;

        if (capture.PlantId.HasValue && capture.PlantId.Value > 0)
        {
            plant = await _supabaseClient.From<PlantDataModel>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, capture.PlantId.Value.ToString()) // int? a string
                .Single();
            
            if (plant?.CropId.HasValue == true && plant.CropId.Value > 0)
            {
                crop = await _supabaseClient.From<CropModel>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, plant.CropId.Value.ToString()) // int? a string
                    .Single();
            }
        }
        
        Data.DTOs.DeviceApi.ThermalDataDto? thermalStats = null;
        if (!string.IsNullOrEmpty(capture.ThermalImageData))
        {
            try
            {
                thermalStats = JsonSerializer.Deserialize<Data.DTOs.DeviceApi.ThermalDataDto>(capture.ThermalImageData,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException jsx)
            {
                _logger.LogWarning(jsx, "No se pudo deserializar ThermalImageData para Detalles ID {ThermalDataId}", capture.Id);
            }
        }

        return Result.Success<ThermalCaptureDetailsDto?>(new ThermalCaptureDetailsDto
        {
            Id = capture.Id,
            DeviceId = capture.DeviceId,
            DeviceName = device?.Name ?? capture.DeviceId.ToString(),
            PlantName = plant?.Name ?? "N/A",
            CropName = crop?.Name ?? "N/A",
            MaxTemp = thermalStats?.Max_Temp ?? 0,
            MinTemp = thermalStats?.Min_Temp ?? 0,
            AvgTemp = thermalStats?.Avg_Temp ?? 0,
            RgbImagePath = capture.RgbImagePath,
            RecordedAt = capture.RecordedAt.ToColombiaTime(),
            Temperatures = thermalStats?.Temperatures,
            ThermalDataJson = capture.ThermalImageData,
            ThermalImageWidth = 32, 
            ThermalImageHeight = 24
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error obteniendo detalles de captura térmica ID: {CaptureId}", captureId);
        // El mensaje de error ya incluye la excepción original, así que no es necesario repetirla completamente.
        return Result.Failure<ThermalCaptureDetailsDto?>($"Error interno al obtener detalles de captura: {ex.Message}");
    }
}

    public async Task<Result<PagedResultDto<DeviceLogDisplayDto>>> GetDeviceLogsAsync(DataQueryFilters filters)
{
    _logger.LogInformation("Obteniendo logs de dispositivo con filtros: {@Filters}", JsonSerializer.Serialize(filters));
    try
    {
        Func<Supabase.Postgrest.Interfaces.IPostgrestTable<DeviceLogModel>> baseQueryBuilder = () =>
        {
            var q = _supabaseClient.From<DeviceLogModel>()
                .Order("log_timestamp_server", Supabase.Postgrest.Constants.Ordering.Descending);

            if (filters.DeviceId.HasValue && filters.DeviceId > 0)
                q = q.Filter("device_id", Supabase.Postgrest.Constants.Operator.Equals, filters.DeviceId.Value);
            if (!string.IsNullOrWhiteSpace(filters.LogLevel))
                q = q.Filter("log_type", Supabase.Postgrest.Constants.Operator.Equals, filters.LogLevel);

            TimeZoneInfo? colombiaZone = null;
            try { colombiaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota"); }
            catch (TimeZoneNotFoundException tzex) { _logger.LogError(tzex, "Zona 'America/Bogota' no encontrada para filtros en GetDeviceLogsAsync."); }

            if (filters.StartDate.HasValue)
            {
                DateTime filterDate = filters.StartDate.Value;
                DateTime startDateUtc;
                if (filterDate.Kind == DateTimeKind.Unspecified && colombiaZone != null) {
                    startDateUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(filterDate, DateTimeKind.Unspecified), colombiaZone);
                } else if (filterDate.Kind == DateTimeKind.Local) {
                    startDateUtc = filterDate.ToUniversalTime();
                } else {
                    startDateUtc = (filterDate.Kind == DateTimeKind.Unspecified) ? filterDate.ToUniversalTime() : filterDate;
                }
                q = q.Filter("log_timestamp_server", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, startDateUtc.ToString("o"));
            }
            if (filters.EndDate.HasValue)
            {
                DateTime filterDate = filters.EndDate.Value;
                DateTime endDateUtc;
                DateTime endOfDayInFilterDate = new DateTime(filterDate.Year, filterDate.Month, filterDate.Day, 23, 59, 59, 999);
                if (filterDate.Kind == DateTimeKind.Unspecified && colombiaZone != null) {
                    endDateUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(endOfDayInFilterDate, DateTimeKind.Unspecified), colombiaZone);
                } else if (filterDate.Kind == DateTimeKind.Local) {
                    endDateUtc = DateTime.SpecifyKind(endOfDayInFilterDate, DateTimeKind.Local).ToUniversalTime();
                } else {
                    endDateUtc = (filterDate.Kind == DateTimeKind.Unspecified) ? DateTime.SpecifyKind(endOfDayInFilterDate, DateTimeKind.Unspecified).ToUniversalTime() : DateTime.SpecifyKind(endOfDayInFilterDate, DateTimeKind.Utc);
                }
                q = q.Filter("log_timestamp_server", Supabase.Postgrest.Constants.Operator.LessThanOrEqual, endDateUtc.ToString("o"));
            }
            return q;
        };

        var countQuery = baseQueryBuilder();
        long totalCount = await countQuery.Count(Supabase.Postgrest.Constants.CountType.Exact);
        _logger.LogInformation("Conteo total de DeviceLog para filtros {@Filters}: {TotalCount}", JsonSerializer.Serialize(filters), totalCount);

        var displayDtos = new List<DeviceLogDisplayDto>();
        if (totalCount > 0) {
            var dataQuery = baseQueryBuilder();
            var (from, to) = ((filters.PageNumber - 1) * filters.PageSize, filters.PageNumber * filters.PageSize - 1);
            var response = await dataQuery.Range(from, to).Get();
            _logger.LogInformation("DeviceLog recuperados de Supabase para página {PageNumber} (tamaño {PageSize}): {Count}", filters.PageNumber, filters.PageSize, response?.Models?.Count ?? 0);

            if (response?.Models != null && response.Models.Any())
            {
                var deviceIds = response.Models.Select(m => m.DeviceId).Distinct().ToList();
                var deviceNames = await GetNamesMapAsync<DeviceDataModel>(deviceIds, "Name"); 

                displayDtos = response.Models.Select(m => new DeviceLogDisplayDto
                {
                    Id = m.Id, 
                    DeviceId = m.DeviceId,
                    DeviceName = deviceNames.TryGetValue(m.DeviceId, out var dn) ? dn : m.DeviceId.ToString(),
                    LogType = m.LogType,
                    LogMessage = m.LogMessage,
                    LogTimestampServer = m.LogTimestampServer.ToColombiaTime(),
                    InternalDeviceTemperature = m.InternalDeviceTemperature, 
                    InternalDeviceHumidity = m.InternalDeviceHumidity     
                }).ToList();
            }
        } else {
                _logger.LogInformation("No se encontraron DeviceLog para los filtros aplicados o el conteo fue cero.");
        }

        return Result.Success(new PagedResultDto<DeviceLogDisplayDto>
        {
            Items = displayDtos,
            PageNumber = filters.PageNumber,
            PageSize = filters.PageSize,
            TotalCount = (int)totalCount
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error obteniendo logs de dispositivo con filtros {@Filters}.", JsonSerializer.Serialize(filters));
        return Result.Failure<PagedResultDto<DeviceLogDisplayDto>>($"Error interno al obtener logs: {ex.Message}");
    }
}

    public async Task<Result<IEnumerable<SensorDataDisplayDto>>> GetAmbientDataForDashboardAsync(TimeSpan duration, int? cropId, int? plantId, int? deviceId)
    {
        _logger.LogInformation("Obteniendo datos ambientales para dashboard. Duración: {Duration}, CropId: {CropId}, PlantId: {PlantId}, DeviceId: {DeviceId}",
            duration, cropId, plantId, deviceId);
        try
        {
            var since = DateTime.UtcNow.Subtract(duration);
            var query = _supabaseClient.From<SensorDataModel>()
                        .Filter("recorded_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, since.ToString("o"))
                        .Order("recorded_at", Supabase.Postgrest.Constants.Ordering.Ascending);

            if (deviceId.HasValue && deviceId > 0)
                query = query.Filter("device_id", Supabase.Postgrest.Constants.Operator.Equals, deviceId.Value);
            else if (plantId.HasValue && plantId > 0)
                query = query.Filter("plant_id", Supabase.Postgrest.Constants.Operator.Equals, plantId.Value);
            else if (cropId.HasValue && cropId > 0)
                query = query.Filter("crop_id", Supabase.Postgrest.Constants.Operator.Equals, cropId.Value);

            var response = await query.Get();
            var displayDtos = new List<SensorDataDisplayDto>();

            if (response?.Models != null && response.Models.Any())
            {
                var deviceIds = response.Models.Select(m => m.DeviceId).Distinct().ToList();
                var deviceNames = await GetNamesMapAsync<DeviceDataModel>(deviceIds, "Name"); // Asumiendo DeviceDataModel.Name

                displayDtos = response.Models.Select(m => new SensorDataDisplayDto
                {
                    // Id no es crucial para el dashboard, pero si m.Id es long, y DTO lo espera, está bien
                    DeviceId = m.DeviceId,
                    DeviceName = deviceNames.TryGetValue(m.DeviceId, out var dn) ? dn : m.DeviceId.ToString(),
                    Light = m.Light,
                    Temperature = m.Temperature,
                    Humidity = m.Humidity,
                    CityTemperature = m.CityTemperature,
                    CityHumidity = m.CityHumidity,
                    IsNight = m.IsNight,
                    RecordedAt = m.RecordedAt.ToColombiaTime()
                }).ToList();
            }
            _logger.LogInformation("Datos ambientales para dashboard recuperados: {Count} puntos.", displayDtos.Count);
            return Result.Success<IEnumerable<SensorDataDisplayDto>>(displayDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo datos ambientales para el dashboard (Duration: {Duration}, CropId: {CropId}, PlantId: {PlantId}, DeviceId: {DeviceId}).", duration, cropId, plantId, deviceId);
            return Result.Failure<IEnumerable<SensorDataDisplayDto>>($"Error interno al obtener datos para el dashboard: {ex.Message}");
        }
    }

    public async Task<Result<ThermalStatsDto>> GetThermalStatsForDashboardAsync(TimeSpan duration, int? cropId, int? plantId, int? deviceId)
    {
        _logger.LogInformation("Obteniendo estadísticas térmicas para dashboard. Duración: {Duration}, CropId: {CropId}, PlantId: {PlantId}, DeviceId: {DeviceId}",
            duration, cropId, plantId, deviceId);
        try
        {
            var since = DateTime.UtcNow.Subtract(duration);
            var query = _supabaseClient.From<ThermalDataModel>()
                        .Filter("recorded_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, since.ToString("o"));

            if (deviceId.HasValue && deviceId > 0)
                query = query.Filter("device_id", Supabase.Postgrest.Constants.Operator.Equals, deviceId.Value);
            else if (plantId.HasValue && plantId > 0)
                query = query.Filter("plant_id", Supabase.Postgrest.Constants.Operator.Equals, plantId.Value);
            else if (cropId.HasValue && cropId > 0)
                query = query.Filter("crop_id", Supabase.Postgrest.Constants.Operator.Equals, cropId.Value);

            // Tomar el más reciente para LatestXyzTemp
            var latestResponse = await query.Order("recorded_at", Supabase.Postgrest.Constants.Ordering.Descending).Limit(1).Get();
            Data.DTOs.DeviceApi.ThermalDataDto? latestStats = null;
            DateTime? latestTimestamp = null;

            if (latestResponse?.Models != null && latestResponse.Models.Any()) {
                var latestModel = latestResponse.Models.First();
                latestTimestamp = latestModel.RecordedAt.ToColombiaTime();
                if (!string.IsNullOrEmpty(latestModel.ThermalImageData)) {
                    try {
                        latestStats = JsonSerializer.Deserialize<Data.DTOs.DeviceApi.ThermalDataDto>(latestModel.ThermalImageData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    } catch (JsonException jsx) { _logger.LogWarning(jsx, "Error deserializando ThermalImageData para el último dato del dashboard, ID: {Id}", latestModel.Id); }
                }
            }

            // Obtener todos los datos en el rango para promedios
            var allResponse = await query.Get(); // No es necesario reordenar si el filtro de tiempo es el mismo

            if (allResponse?.Models == null || !allResponse.Models.Any())
            {
                _logger.LogInformation("No hay datos térmicos recientes para los filtros del dashboard (Duration: {Duration}, CropId: {CropId}, PlantId: {PlantId}, DeviceId: {DeviceId}).", duration, cropId, plantId, deviceId);
                return Result.Success(new ThermalStatsDto()); // Devuelve DTO vacío pero exitoso
            }

            var thermalStatsList = new List<Data.DTOs.DeviceApi.ThermalDataDto>();
            foreach (var model in allResponse.Models)
            {
                if (string.IsNullOrEmpty(model.ThermalImageData)) continue;
                try
                {
                    var stats = JsonSerializer.Deserialize<Data.DTOs.DeviceApi.ThermalDataDto>(model.ThermalImageData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (stats != null) thermalStatsList.Add(stats);
                }
                catch (JsonException jsx) { _logger.LogWarning(jsx, "Error deserializando ThermalImageData para dashboard, ID: {Id}", model.Id); }
            }

            if (!thermalStatsList.Any())
            {
                _logger.LogInformation("No se pudieron deserializar datos térmicos válidos para estadísticas del dashboard (Duration: {Duration}, CropId: {CropId}, PlantId: {PlantId}, DeviceId: {DeviceId}).", duration, cropId, plantId, deviceId);
                return Result.Success(new ThermalStatsDto()); // Devuelve DTO vacío pero exitoso
            }

            var dashboardStats = new ThermalStatsDto
            {
                AverageMaxTemp24h = thermalStatsList.Average(s => s.Max_Temp),
                AverageMinTemp24h = thermalStatsList.Average(s => s.Min_Temp),
                AverageAvgTemp24h = thermalStatsList.Average(s => s.Avg_Temp),
                LatestMaxTemp = latestStats?.Max_Temp ?? 0,
                LatestMinTemp = latestStats?.Min_Temp ?? 0,
                LatestAvgTemp = latestStats?.Avg_Temp ?? 0,
                LatestThermalReadingTimestamp = latestTimestamp?.ToColombiaTime()
            };
            _logger.LogInformation("Estadísticas térmicas para dashboard calculadas exitosamente (Duration: {Duration}, CropId: {CropId}, PlantId: {PlantId}, DeviceId: {DeviceId}).", duration, cropId, plantId, deviceId);
            return Result.Success(dashboardStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo estadísticas térmicas para el dashboard (Duration: {Duration}, CropId: {CropId}, PlantId: {PlantId}, DeviceId: {DeviceId}).", duration, cropId, plantId, deviceId);
            return Result.Failure<ThermalStatsDto>($"Error interno al obtener estadísticas térmicas: {ex.Message}");
        }
    }

    public async Task<Result<int>> GetActiveDevicesCountAsync(int? cropId, int? plantId)
    {
        try
        {
            var activeStatusResult = await GetStatusIdAsync(StatusNameActive);
            if (activeStatusResult.IsFailure)
            {
                _logger.LogError("Fallo al obtener ID de estado ACTIVO para contar dispositivos: {Error}", activeStatusResult.ErrorMessage);
                return Result.Failure<int>(activeStatusResult.ErrorMessage);
            }
            int activeStatusId = activeStatusResult.Value;

            var query = _supabaseClient.From<DeviceDataModel>().Filter("status_id", Supabase.Postgrest.Constants.Operator.Equals, activeStatusId);

            if (plantId.HasValue && plantId > 0)
                query = query.Filter("plant_id", Supabase.Postgrest.Constants.Operator.Equals, plantId.Value);
            else if (cropId.HasValue && cropId > 0) // Solo aplicar filtro de cultivo si no hay filtro de planta
                query = query.Filter("crop_id", Supabase.Postgrest.Constants.Operator.Equals, cropId.Value);


            var count = await query.Count(Supabase.Postgrest.Constants.CountType.Exact);
            _logger.LogInformation("Conteo de dispositivos activos (CropId: {CropId}, PlantId: {PlantId}): {Count}", cropId, plantId, count);
            return Result.Success((int)count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error contando dispositivos activos (CropId: {CropId}, PlantId: {PlantId}).", cropId, plantId);
            return Result.Failure<int>($"Error interno al contar dispositivos activos: {ex.Message}");
        }
    }

    public async Task<Result<int>> GetMonitoredPlantsCountAsync(int? cropId)
    {
        try
        {
            var activeStatusResult = await GetStatusIdAsync(StatusNameActive);
            if (activeStatusResult.IsFailure)
            {
                _logger.LogError("Fallo al obtener ID de estado ACTIVO para contar plantas: {Error}", activeStatusResult.ErrorMessage);
                return Result.Failure<int>(activeStatusResult.ErrorMessage);
            }
            int activeStatusId = activeStatusResult.Value;

            var query = _supabaseClient.From<PlantDataModel>().Filter("status_id", Supabase.Postgrest.Constants.Operator.Equals, activeStatusId);

            if (cropId.HasValue && cropId > 0)
                query = query.Filter("crop_id", Supabase.Postgrest.Constants.Operator.Equals, cropId.Value);

            var count = await query.Count(Supabase.Postgrest.Constants.CountType.Exact);
            _logger.LogInformation("Conteo de plantas monitoreadas (CropId: {CropId}): {Count}", cropId, count);
            return Result.Success((int)count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error contando plantas monitoreadas (CropId: {CropId}).", cropId);
            return Result.Failure<int>($"Error interno al contar plantas: {ex.Message}");
        }
    }
    
    public async Task<Result<SensorDataDisplayDto?>> GetLatestAmbientDataAsync(int? cropId, int? plantId, int? deviceId)
{
    _logger.LogInformation("Obteniendo última lectura ambiental para CropId: {CropId}, PlantId: {PlantId}, DeviceId: {DeviceId}", cropId, plantId, deviceId);
    try
    {
        var query = _supabaseClient.From<SensorDataModel>()
                        .Order("recorded_at", Supabase.Postgrest.Constants.Ordering.Descending);

        // Aplicar filtros jerárquicamente: DeviceId tiene precedencia, luego PlantId, luego CropId
        if (deviceId.HasValue && deviceId > 0)
        {
            query = query.Filter("device_id", Supabase.Postgrest.Constants.Operator.Equals, deviceId.Value);
        }
        else if (plantId.HasValue && plantId > 0)
        {
            query = query.Filter("plant_id", Supabase.Postgrest.Constants.Operator.Equals, plantId.Value);
        }
        else if (cropId.HasValue && cropId > 0)
        {
            query = query.Filter("crop_id", Supabase.Postgrest.Constants.Operator.Equals, cropId.Value);
        }

        var response = await query.Limit(1).Single(); // Single() para obtener un solo objeto o null

        if (response == null)
        {
            _logger.LogInformation("No se encontró última lectura ambiental para los filtros aplicados.");
            return Result.Success<SensorDataDisplayDto?>(null);
        }

        // Mapear a SensorDataDisplayDto
        // Necesitamos nombres para Device, Plant, Crop si están presentes
        string deviceName = response.DeviceId.ToString();
        if (response.DeviceId > 0)
        {
            var deviceNameMap = await GetNamesMapAsync<DeviceDataModel>(new List<int> { response.DeviceId }, "Name");
            if (deviceNameMap.TryGetValue(response.DeviceId, out var dn))
            {
                deviceName = dn;
            }
        }

        string? plantName = "N/A";
        if (response.PlantId.HasValue && response.PlantId > 0)
        {
            var plantNameMap = await GetNamesMapAsync<PlantDataModel>(new List<int> { response.PlantId.Value }, "Name");
            if (plantNameMap.TryGetValue(response.PlantId.Value, out var pn))
            {
                plantName = pn;
            }
        }

        string? cropName = "N/A";
        if (response.CropId.HasValue && response.CropId > 0)
        {
            var cropNameMap = await GetNamesMapAsync<CropModel>(new List<int> { response.CropId.Value }, "Name");
            if (cropNameMap.TryGetValue(response.CropId.Value, out var cn))
            {
                cropName = cn;
            }
        }

        var displayDto = new SensorDataDisplayDto
        {
            Id = response.Id,
            DeviceId = response.DeviceId,
            DeviceName = deviceName,
            PlantName = plantName,
            CropName = cropName,
            Light = response.Light,
            Temperature = response.Temperature,
            Humidity = response.Humidity,
            CityTemperature = response.CityTemperature,
            CityHumidity = response.CityHumidity,
            CityWeatherCondition = response.CityWeatherCondition,
            IsNight = response.IsNight,
            RecordedAt = response.RecordedAt.ToColombiaTime() // Convertir a Hora Colombia
        };
        _logger.LogInformation("Última lectura ambiental obtenida: {@DisplayDto}", JsonSerializer.Serialize(displayDto));
        return Result.Success<SensorDataDisplayDto?>(displayDto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error obteniendo última lectura ambiental para CropId: {CropId}, PlantId: {PlantId}, DeviceId: {DeviceId}", cropId, plantId, deviceId);
        return Result.Failure<SensorDataDisplayDto?>($"Error interno al obtener última lectura ambiental: {ex.Message}");
    }
}
}