using System.Globalization;
using System.Text.Json;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;
using ArandanoIRT.Web._1_Application.DTOs.Common;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
using ArandanoIRT.Web._1_Application.DTOs.SensorData;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._3_Presentation.ViewModels.SensorData;
using CsvHelper;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class DataQueryService : IDataQueryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataQueryService> _logger;

    public DataQueryService(ApplicationDbContext context, ILogger<DataQueryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PagedResultDto<SensorDataDisplayDto>>> GetSensorDataAsync(DataQueryFilters filters)
    {
        _logger.LogInformation("Obteniendo datos de sensores con filtros: {@Filters}", filters);
        try
        {
            IQueryable<EnvironmentalReading> query = _context.EnvironmentalReadings.AsNoTracking()
                .Include(er => er.Device)
                .Include(er => er.Plant)
                .ThenInclude(p => p.Crop);

            if (filters.DeviceId.HasValue)
            {
                query = query.Where(er => er.DeviceId == filters.DeviceId.Value);
            }

            if (filters.PlantId.HasValue)
            {
                query = query.Where(er => er.PlantId == filters.PlantId.Value);
            }
            else if (filters.CropId.HasValue)
            {
                query = query.Where(er => er.Plant != null && er.Plant.CropId == filters.CropId.Value);
            }

            query = query.ApplyDateFilters(filters, er => er.RecordedAtServer);

            var totalCount = await query.CountAsync();
            if (totalCount == 0)
                return Result.Success(new PagedResultDto<SensorDataDisplayDto>
                {
                    Items = new List<SensorDataDisplayDto>(),
                    TotalCount = 0,
                    PageNumber = filters.PageNumber,
                    PageSize = filters.PageSize
                });

            var rawData = await query
                .OrderByDescending(er => er.RecordedAtServer)
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(er => new
                {
                    er.Id,
                    er.DeviceId,
                    DeviceName = er.Device.Name,
                    PlantName = er.Plant != null ? er.Plant.Name : "N/A",
                    CropName = er.Plant != null ? er.Plant.Crop.Name : "N/A",
                    er.Temperature,
                    er.Humidity,
                    er.ExtraData,
                    er.CityTemperature,
                    er.CityHumidity,
                    er.CityWeatherCondition,
                    er.RecordedAtServer,
                    er.RecordedAtDevice
                })
                .ToListAsync();

            var finalData = rawData.Select(er =>
            {
                float? light = null;
                var otherData = new Dictionary<string, JsonElement>();
                var keyTranslations = new Dictionary<string, string>
                {
                    { "pressure", "Presión (hPa)" }
                };

                if (!string.IsNullOrWhiteSpace(er.ExtraData))
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(er.ExtraData);
                        foreach (var property in jsonDoc.RootElement.EnumerateObject())
                        {
                            if (property.NameEquals("is_night")) continue;
                            if (property.NameEquals("light") && property.Value.TryGetSingle(out var lightValue))
                            {
                                light = lightValue;
                            }
                            else
                            {
                                var displayName = keyTranslations.GetValueOrDefault(property.Name, property.Name);
                                otherData[displayName] = property.Value.Clone();
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "No se pudo parsear ExtraData para el registro ID {Id}", er.Id);
                    }

                return new SensorDataDisplayDto
                {
                    Id = er.Id,
                    DeviceName = er.DeviceName,
                    PlantName = er.PlantName,
                    Temperature = er.Temperature,
                    Humidity = er.Humidity,
                    CityTemperature = er.CityTemperature,
                    CityHumidity = er.CityHumidity,
                    CityWeatherCondition = er.CityWeatherCondition,
                    Light = light,
                    OtherData = otherData.Any() ? otherData : null,
                    RecordedAt = er.RecordedAtDevice ?? er.RecordedAtServer
                };
            }).ToList();

            return Result.Success(new PagedResultDto<SensorDataDisplayDto>
            {
                Items = finalData,
                TotalCount = totalCount,
                PageNumber = filters.PageNumber,
                PageSize = filters.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo datos de sensores con filtros {@Filters}.", filters);
            return Result.Failure<PagedResultDto<SensorDataDisplayDto>>(
                $"Error interno al obtener datos de sensores: {ex.Message}");
        }
    }

    public async Task<Result<PagedResultDto<ThermalCaptureSummaryDto>>> GetThermalCapturesAsync(
    DataQueryFilters filters)
    {
        _logger.LogInformation("Obteniendo capturas térmicas con filtros: {@Filters}", filters);
        try
        {
            IQueryable<ThermalCapture> query = _context.ThermalCaptures.AsNoTracking()
                .Include(tc => tc.Device)
                .Include(tc => tc.Plant)
                .ThenInclude(p => p.Crop);

            if (filters.DeviceId.HasValue)
            {
                query = query.Where(tc => tc.DeviceId == filters.DeviceId.Value);
            }

            if (filters.PlantId.HasValue)
            {
                query = query.Where(tc => tc.PlantId == filters.PlantId.Value);
            }
            else if (filters.CropId.HasValue)
            {
                query = query.Where(tc => tc.Plant != null && tc.Plant.CropId == filters.CropId.Value);
            }

            query = query.ApplyDateFilters(filters, tc => tc.RecordedAtServer);

            var totalCount = await query.CountAsync();
            if (totalCount == 0)
                return Result.Success(new PagedResultDto<ThermalCaptureSummaryDto>
                {
                    Items = new List<ThermalCaptureSummaryDto>(),
                    TotalCount = 0,
                    PageNumber = filters.PageNumber,
                    PageSize = filters.PageSize
                });

            var rawData = await query
                .OrderByDescending(tc => tc.RecordedAtServer)
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(tc => new
                {
                    tc.Id,
                    tc.DeviceId,
                    DeviceName = tc.Device.Name,
                    PlantName = tc.Plant != null ? tc.Plant.Name : "N/A",
                    tc.ThermalDataStats,
                    tc.RgbImagePath,
                    tc.RecordedAtServer,
                    tc.RecordedAtDevice
                })
                .ToListAsync();

            var finalData = rawData.Select(m =>
            {
                var thermalStats = DeserializeThermalStats(m.ThermalDataStats, m.Id);
                return new ThermalCaptureSummaryDto
                {
                    Id = m.Id,
                    DeviceId = m.DeviceId,
                    DeviceName = m.DeviceName,
                    PlantName = m.PlantName,
                    MaxTemp = thermalStats?.Max_Temp,
                    MinTemp = thermalStats?.Min_Temp,
                    AvgTemp = thermalStats?.Avg_Temp,
                    RgbImagePath = m.RgbImagePath,
                    RecordedAt = m.RecordedAtDevice ?? m.RecordedAtServer
                };
            }).ToList();

            return Result.Success(new PagedResultDto<ThermalCaptureSummaryDto>
            {
                Items = finalData,
                TotalCount = totalCount,
                PageNumber = filters.PageNumber,
                PageSize = filters.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo capturas térmicas con filtros {@Filters}.", filters);
            return Result.Failure<PagedResultDto<ThermalCaptureSummaryDto>>(
                $"Error interno al obtener capturas térmicas: {ex.Message}");
        }
    }

    public async Task<Result<ThermalCaptureDetailsDto?>> GetThermalCaptureDetailsAsync(long captureId)
    {
        _logger.LogInformation("Obteniendo detalles de captura térmica ID: {CaptureId}", captureId);
        try
        {
            var result = await _context.ThermalCaptures
                .AsNoTracking()
                .Where(tc => tc.Id == captureId)
                .Select(tc => new // Proyección para traer solo lo necesario
                {
                    Capture = tc,
                    DeviceName = tc.Device.Name,
                    PlantName = tc.Plant != null ? tc.Plant.Name : "N/A",
                    CropName = tc.Plant != null ? tc.Plant.Crop.Name : tc.Device.Crop.Name ?? "N/A"
                })
                .FirstOrDefaultAsync();

            if (result == null)
            {
                _logger.LogWarning("No se encontró captura térmica con ID: {CaptureId}", captureId);
                return Result.Success<ThermalCaptureDetailsDto?>(null);
            }

            var thermalStats = DeserializeThermalStats(result.Capture.ThermalDataStats, result.Capture.Id);

            var detailsDto = new ThermalCaptureDetailsDto
            {
                Id = result.Capture.Id,
                DeviceId = result.Capture.DeviceId,
                DeviceName = result.DeviceName,
                PlantName = result.PlantName,
                CropName = result.CropName,
                MaxTemp = thermalStats?.Max_Temp ?? 0,
                MinTemp = thermalStats?.Min_Temp ?? 0,
                AvgTemp = thermalStats?.Avg_Temp ?? 0,
                RgbImagePath = result.Capture.RgbImagePath,
                RecordedAt = result.Capture.RecordedAtServer,
                Temperatures = thermalStats?.Temperatures,
                ThermalDataJson = result.Capture.ThermalDataStats,
                ThermalImageWidth = 32,
                ThermalImageHeight = 24
            };

            return Result.Success<ThermalCaptureDetailsDto?>(detailsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo detalles de captura térmica ID: {CaptureId}", captureId);
            return Result.Failure<ThermalCaptureDetailsDto?>(
                $"Error interno al obtener detalles de captura: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<SensorDataDisplayDto>>> GetAmbientDataForDashboardAsync(TimeSpan duration,
        int? cropId, int? plantId)
    {
        _logger.LogInformation("Obteniendo datos ambientales para dashboard. Duración: {Duration}, etc.", duration);
        try
        {
            var since = DateTime.UtcNow.Subtract(duration);
            var query = _context.EnvironmentalReadings.AsNoTracking().Where(er => er.RecordedAtServer >= since);

            if (plantId.HasValue) query = query.Where(er => er.PlantId == plantId.Value);
            else if (cropId.HasValue) query = query.Where(er => er.Device.CropId == cropId.Value);

            // 1. Traer datos crudos
            var rawData = await query
                .OrderBy(er => er.RecordedAtServer)
                .Take(100)
                .Select(er => new
                {
                    er.DeviceId,
                    DeviceName = er.Device.Name,
                    er.ExtraData,
                    er.Temperature,
                    er.Humidity,
                    er.CityTemperature,
                    er.CityHumidity,
                    er.RecordedAtServer
                })
                .ToListAsync();

            // 2. Transformar en memoria
            var finalData = rawData.Select(er => new SensorDataDisplayDto
            {
                DeviceId = er.DeviceId,
                DeviceName = er.DeviceName,
                Light = GetLightValueFromJson(er.ExtraData),
                Temperature = er.Temperature,
                Humidity = er.Humidity,
                CityTemperature = er.CityTemperature,
                CityHumidity = er.CityHumidity,
                IsNight = er.ExtraData != null && er.ExtraData.Contains("\"is_night\": true"),
                RecordedAt = er.RecordedAtServer
            }).ToList();

            _logger.LogInformation("Datos ambientales para dashboard recuperados: {Count} puntos.", finalData.Count);
            return Result.Success<IEnumerable<SensorDataDisplayDto>>(finalData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo datos ambientales para el dashboard.");
            return Result.Failure<IEnumerable<SensorDataDisplayDto>>(
                $"Error interno al obtener datos para el dashboard: {ex.Message}");
        }
    }

    public async Task<Result<ThermalStatsDto>> GetThermalStatsForDashboardAsync(TimeSpan duration, int? cropId,
        int? plantId)
    {
        _logger.LogInformation("Obteniendo estadísticas térmicas para dashboard. Duración: {Duration}", duration);
        try
        {
            var since = DateTime.UtcNow.Subtract(duration);
            var query = _context.ThermalCaptures.AsNoTracking().Where(tc => tc.RecordedAtServer >= since);

            // Aplicar filtros (esta lógica no cambia)
            if (plantId.HasValue) query = query.Where(tc => tc.PlantId == plantId.Value);
            else if (cropId.HasValue) query = query.Where(tc => tc.Device.CropId == cropId.Value);

            // 1. Traer solo el JSON y la fecha, sin procesar nada.
            var rawCaptures = await query
                .OrderBy(tc => tc.RecordedAtServer)
                .Take(100)
                .Select(tc => new { tc.Id, tc.RecordedAtServer, tc.ThermalDataStats })
                .ToListAsync();

            if (!rawCaptures.Any())
            {
                _logger.LogInformation("No hay datos térmicos recientes para los filtros del dashboard.");
                return Result.Success(new ThermalStatsDto());
            }

            // 2. Deserializar toda la lista en memoria.
            var thermalStatsList = new List<ThermalDataDto>();
            foreach (var model in rawCaptures)
            {
                var stats = DeserializeThermalStats(model.ThermalDataStats, model.Id);
                if (stats != null) thermalStatsList.Add(stats);
            }

            if (!thermalStatsList.Any())
            {
                _logger.LogWarning(
                    "Se encontraron capturas térmicas pero no se pudieron deserializar sus estadísticas.");
                return Result.Success(new ThermalStatsDto());
            }

            // 3. Calcular estadísticas sobre la lista ya procesada.
            var latestCapture = rawCaptures.OrderByDescending(x => x.RecordedAtServer).First();
            var latestStats = thermalStatsList.LastOrDefault();

            var dashboardStats = new ThermalStatsDto
            {
                AverageMaxTemp24h = thermalStatsList.Average(s => s.Max_Temp),
                AverageMinTemp24h = thermalStatsList.Average(s => s.Min_Temp),
                AverageAvgTemp24h = thermalStatsList.Average(s => s.Avg_Temp),
                LatestMaxTemp = latestStats?.Max_Temp,
                LatestMinTemp = latestStats?.Min_Temp,
                LatestAvgTemp = latestStats?.Avg_Temp,
                LatestThermalReadingTimestamp = latestCapture.RecordedAtServer
            };

            return Result.Success(dashboardStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo estadísticas térmicas para el dashboard.");
            return Result.Failure<ThermalStatsDto>($"Error interno al obtener estadísticas térmicas: {ex.Message}");
        }
    }

    public async Task<Result<int>> GetActiveDevicesCountAsync(int? cropId, int? plantId)
    {
        try
        {
            var query = _context.Devices.AsNoTracking()
                .Where(d => d.Status == DeviceStatus.ACTIVE); // Consulta directa con el Enum

            if (plantId.HasValue)
                query = query.Where(d => d.PlantId == plantId.Value);
            else if (cropId.HasValue)
                query = query.Where(d => d.CropId == cropId.Value);

            var count = await query.CountAsync();
            _logger.LogInformation("Conteo de dispositivos activos (CropId: {CropId}, PlantId: {PlantId}): {Count}",
                cropId, plantId, count);
            return Result.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error contando dispositivos activos.");
            return Result.Failure<int>($"Error interno al contar dispositivos activos: {ex.Message}");
        }
    }

    public async Task<Result<int>> GetMonitoredPlantsCountAsync(int? cropId)
    {
        try
        {
            // Lógica ajustada: Una planta se considera "monitoreada" si tiene al menos un dispositivo ACTIVO asociado.
            // La entidad Plant ya no tiene un campo 'status'.
            var query = _context.Plants.AsNoTracking();

            if (cropId.HasValue)
                query = query.Where(p => p.CropId == cropId.Value);

            // Contar solo las plantas que tienen algún dispositivo en estado ACTIVO.
            var count = await query.CountAsync(p => p.Devices.Any(d => d.Status == DeviceStatus.ACTIVE));

            _logger.LogInformation("Conteo de plantas monitoreadas (CropId: {CropId}): {Count}", cropId, count);
            return Result.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error contando plantas monitoreadas (CropId: {CropId}).", cropId);
            return Result.Failure<int>($"Error interno al contar plantas monitoreadas: {ex.Message}");
        }
    }

    public async Task<Result<SensorDataDisplayDto?>> GetLatestAmbientDataAsync(int? cropId, int? plantId, int? deviceId)
    {
        _logger.LogInformation("Obteniendo última lectura ambiental para CropId: {CropId}, etc.", cropId);
        try
        {
            var query = _context.EnvironmentalReadings.AsNoTracking();

            // Filtros jerárquicos
            if (deviceId.HasValue) query = query.Where(er => er.DeviceId == deviceId.Value);
            else if (plantId.HasValue) query = query.Where(er => er.PlantId == plantId.Value);
            else if (cropId.HasValue) query = query.Where(er => er.Device.CropId == cropId.Value);

            // 1. Traer el último registro crudo
            var rawResult = await query
                .OrderByDescending(er => er.RecordedAtServer)
                .Select(er => new
                {
                    er.Id,
                    er.DeviceId,
                    DeviceName = er.Device.Name,
                    PlantName = er.Plant != null ? er.Plant.Name : "N/A",
                    CropName = er.Device.Crop.Name,
                    er.Temperature,
                    er.Humidity,
                    er.ExtraData,
                    er.CityTemperature,
                    er.CityHumidity,
                    er.CityWeatherCondition,
                    er.RecordedAtServer
                })
                .FirstOrDefaultAsync();

            if (rawResult == null) return Result.Success<SensorDataDisplayDto?>(null);

            // 2. Transformar el objeto en memoria
            var finalResult = new SensorDataDisplayDto
            {
                Id = rawResult.Id,
                DeviceId = rawResult.DeviceId,
                DeviceName = rawResult.DeviceName,
                PlantName = rawResult.PlantName,
                CropName = rawResult.CropName,
                Light = GetLightValueFromJson(rawResult.ExtraData), // Llamada segura
                Temperature = rawResult.Temperature,
                Humidity = rawResult.Humidity,
                CityTemperature = rawResult.CityTemperature,
                CityHumidity = rawResult.CityHumidity,
                CityWeatherCondition = rawResult.CityWeatherCondition,
                IsNight = rawResult.ExtraData != null && rawResult.ExtraData.Contains("\"is_night\": true"),
                RecordedAt = rawResult.RecordedAtServer
            };

            return Result.Success(finalResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo última lectura ambiental.");
            return Result.Failure<SensorDataDisplayDto?>(
                $"Error interno al obtener última lectura ambiental: {ex.Message}");
        }
    }

    public async Task<Result<List<PlantRawDataDto>>> GetRawDataForAnalysisAsync(List<int> plantIds, DateTime startTime,
        DateTime endTime)
    {
        try
        {
            var plantsWithData = await _context.Plants
                .AsNoTracking()
                .Include(p =>
                    p.EnvironmentalReadings.Where(er =>
                        er.RecordedAtServer >= startTime && er.RecordedAtServer < endTime))
                .Include(p =>
                    p.ThermalCaptures.Where(tc => tc.RecordedAtServer >= startTime && tc.RecordedAtServer < endTime))
                .Where(p => plantIds.Contains(p.Id))
                .Select(p => new PlantRawDataDto
                {
                    Plant = p,
                    EnvironmentalReadings = p.EnvironmentalReadings.ToList(),
                    ThermalCaptures = p.ThermalCaptures.ToList()
                })
                .ToListAsync();

            return Result.Success(plantsWithData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo datos crudos para el análisis.");
            return Result.Failure<List<PlantRawDataDto>>($"Error interno al obtener datos para análisis: {ex.Message}");
        }
    }

    public async Task<Result<(ThermalDataDto? Stats, string? ImagePath)>> GetLatestCaptureForMaskAsync(int plantId)
    {
        try
        {
            var latestCapture = await _context.ThermalCaptures
                .AsNoTracking()
                .Where(tc =>
                    tc.PlantId == plantId && tc.RgbImagePath != null &&
                    EF.Functions.JsonExists(tc.ThermalDataStats, "temperatures"))
                .OrderByDescending(tc => tc.RecordedAtServer)
                .Select(tc => new { tc.ThermalDataStats, tc.RgbImagePath }) // Seleccionamos ambos campos
                .FirstOrDefaultAsync();

            if (latestCapture == null) return Result.Success<(ThermalDataDto? Stats, string? ImagePath)>((null, null));

            var thermalStats = DeserializeThermalStats(latestCapture.ThermalDataStats, 0);
            return Result.Success((thermalStats, latestCapture.RgbImagePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo la última captura para la máscara de la planta {PlantId}", plantId);
            return Result.Failure<(ThermalDataDto? Stats, string? ImagePath)>(
                "Error interno al obtener datos de la captura.");
        }
    }

    public async Task<byte[]> GetAmbientDataAsCsvAsync(DataQueryFilters filters)
    {
        _logger.LogInformation("Generando CSV de datos de sensores con filtros: {@Filters}", filters);

        // 1. Construimos la consulta con los mismos filtros que la vista principal
        var query = _context.EnvironmentalReadings.AsNoTracking();

        if (filters.DeviceId.HasValue) query = query.Where(er => er.DeviceId == filters.DeviceId.Value);
        if (filters.PlantId.HasValue) query = query.Where(er => er.PlantId == filters.PlantId.Value);
        if (filters.CropId.HasValue) query = query.Where(er => er.Device.CropId == filters.CropId.Value);
        query = query.ApplyDateFilters(filters, er => er.RecordedAtServer);

        // 2. Ejecutamos la consulta SIN PAGINACIÓN y proyectamos a un modelo simple para el CSV
        var dataToExport = await query
            .OrderByDescending(er => er.RecordedAtServer)
            .Select(er => new
            {
                FechaRegistro = er.RecordedAtDevice ?? er.RecordedAtServer,
                Dispositivo = er.Device.Name,
                Planta = er.Plant != null ? er.Plant.Name : "N/A",
                TemperaturaSensor = er.Temperature,
                HumedadSensor = er.Humidity,
                TemperaturaCiudad = er.CityTemperature,
                HumedadCiudad = er.CityHumidity,
                ClimaCiudad = er.CityWeatherCondition,
                DatosExtra = er.ExtraData
            })
            .ToListAsync();

        // 3. Usamos CsvHelper para escribir los datos en un stream en memoria
        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.GetCultureInfo("es-CO"))) // Usamos cultura local para formatos
        {
            // Escribe las cabeceras y los registros
            csv.WriteRecords(dataToExport);
        }

        // 4. Devolvemos los bytes del archivo generado
        return memoryStream.ToArray();
    }

    public async Task<byte[]> GetThermalCapturesAsCsvAsync(DataQueryFilters filters)
    {
        _logger.LogInformation("Generando CSV de capturas térmicas con filtros: {@Filters}", filters);

        // 1. Construimos la consulta con los mismos filtros
        var query = _context.ThermalCaptures.AsNoTracking();

        if (filters.DeviceId.HasValue) query = query.Where(tc => tc.DeviceId == filters.DeviceId.Value);
        if (filters.PlantId.HasValue) query = query.Where(tc => tc.PlantId == filters.PlantId.Value);
        if (filters.CropId.HasValue) query = query.Where(tc => tc.Device.CropId == filters.CropId.Value);
        query = query.ApplyDateFilters(filters, tc => tc.RecordedAtServer);

        // 2. Ejecutamos la consulta SIN PAGINACIÓN y proyectamos a un modelo simple
        var rawData = await query
            .OrderByDescending(tc => tc.RecordedAtServer)
            .Select(tc => new
            {
                tc.Id,
                tc.RecordedAtServer,
                DeviceName = tc.Device.Name,
                PlantName = tc.Plant != null ? tc.Plant.Name : "N/A",
                tc.ThermalDataStats,
                tc.RgbImagePath
            })
            .ToListAsync();

        // Procesar en memoria
        var dataToExport = rawData.Select(m =>
        {
            var stats = DeserializeThermalStats(m.ThermalDataStats, m.Id);
            return new
            {
                IdCaptura = m.Id,
                FechaRegistro = m.RecordedAtServer,
                Dispositivo = m.DeviceName,
                Planta = m.PlantName,
                TempMax = stats?.Max_Temp,
                TempMin = stats?.Min_Temp,
                TempPromedio = stats?.Avg_Temp,
                Temperaturas = stats?.Temperatures != null
                    ? string.Join(",", stats.Temperatures)
                    : string.Empty,
                ImagenRGB = m.RgbImagePath
            };
        }).ToList();
        // 3. Usamos CsvHelper para escribir los datos en memoria
        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.GetCultureInfo("es-CO")))
        {
            csv.WriteRecords(dataToExport);
        }

        // 4. Devolvemos los bytes del archivo generado
        return memoryStream.ToArray();
    }

    #region Métodos Auxiliares Internos

    /// <summary>
    ///     Parsea de forma segura el campo ExtraData (JSON) para extraer el valor de 'light'.
    /// </summary>
    /// <returns>El valor de 'light' o null si no se encuentra o hay un error.</returns>
    private float? GetLightValueFromJson(string? extraDataJson)
    {
        if (string.IsNullOrWhiteSpace(extraDataJson)) return null;
        try
        {
            using var jsonDoc = JsonDocument.Parse(extraDataJson);
            if (jsonDoc.RootElement.TryGetProperty("light", out var lightElement) &&
                lightElement.TryGetSingle(out var lightValue))
                return lightValue;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "No se pudo parsear el JSON de ExtraData. Contenido: {Json}", extraDataJson);
        }

        return null;
    }

    private ThermalDataDto? DeserializeThermalStats(string? thermalDataJson, long entityId)
    {
        if (string.IsNullOrEmpty(thermalDataJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<ThermalDataDto>(thermalDataJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "No se pudo deserializar ThermalDataStats para la entidad con ID {Id}", entityId);
            return null;
        }
    }

    #endregion
}