using System.Text.Json;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web.Common;
using ArandanoIRT.Web._2_Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class DataQueryService : IDataQueryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataQueryService> _logger;
    private readonly TimeZoneInfo _colombiaZone;

    public DataQueryService(ApplicationDbContext context, ILogger<DataQueryService> logger)
    {
        _context = context;
        _logger = logger;
        try
        {
            _colombiaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        }
        catch (TimeZoneNotFoundException tzex)
        {
            _logger.LogCritical(tzex, "La zona horaria 'America/Bogota' no se encontró en el sistema. Las conversiones de fecha pueden ser incorrectas.");
            // Fallback a la zona UTC si no se encuentra la de Colombia para evitar que la aplicación falle al iniciar.
            _colombiaZone = TimeZoneInfo.Utc;
        }
    }

    #region Métodos Auxiliares Internos

    /// <summary>
    /// Convierte una fecha y hora a la zona horaria de Colombia.
    /// Asume que la fecha de entrada está en UTC.
    /// </summary>
    private DateTime ToColombiaTime(DateTime utcDate)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDate, _colombiaZone);
    }

    /// <summary>
    /// Parsea de forma segura el campo ExtraData (JSON) para extraer el valor de 'light'.
    /// </summary>
    /// <returns>El valor de 'light' o null si no se encuentra o hay un error.</returns>
    private float? GetLightValueFromJson(string? extraDataJson)
    {
        if (string.IsNullOrWhiteSpace(extraDataJson))
        {
            return null;
        }

        try
        {
            using var jsonDoc = JsonDocument.Parse(extraDataJson);
            if (jsonDoc.RootElement.TryGetProperty("light", out var lightElement) && lightElement.TryGetSingle(out var lightValue))
            {
                return lightValue;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "No se pudo parsear el JSON de ExtraData para obtener 'light'. Contenido: {Json}", extraDataJson);
        }
        return null;
    }

    /// <summary>
    /// Parsea de forma segura el campo de estadísticas térmicas (JSON).
    /// </summary>
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

    public async Task<Result<PagedResultDto<SensorDataDisplayDto>>> GetSensorDataAsync(DataQueryFilters filters)
    {
        _logger.LogInformation("Obteniendo datos de sensores con filtros: {@Filters}", filters);
        try
        {
            var query = _context.EnvironmentalReadings.AsNoTracking();

            // Aplicar filtros
            if (filters.DeviceId.HasValue)
                query = query.Where(er => er.DeviceId == filters.DeviceId.Value);
            if (filters.PlantId.HasValue)
                query = query.Where(er => er.PlantId == filters.PlantId.Value);
            if (filters.CropId.HasValue)
                // EnvironmentalReading no tiene CropId, se filtra a través del Dispositivo
                query = query.Where(er => er.Device.CropId == filters.CropId.Value);

            query = query.ApplyDateFilters(filters, er => er.RecordedAtServer);

            // Conteo total para paginación
            var totalCount = await query.CountAsync();
            if (totalCount == 0)
            {
                return Result.Success(new PagedResultDto<SensorDataDisplayDto>
                {
                    Items = new List<SensorDataDisplayDto>(),
                    PageNumber = filters.PageNumber,
                    PageSize = filters.PageSize,
                    TotalCount = 0
                });
            }

            // Aplicar orden y paginación
            var readings = await query
                .OrderByDescending(er => er.RecordedAtServer)
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(er => new SensorDataDisplayDto
                {
                    Id = er.Id,
                    DeviceId = er.DeviceId,
                    DeviceName = er.Device.Name, // Navegación directa
                    PlantName = er.Plant != null ? er.Plant.Name : "N/A", // Navegación directa
                    CropName = er.Device.Crop.Name, // Navegación anidada
                    Light = GetLightValueFromJson(er.ExtraData), // Extracción desde JSON
                    Temperature = er.Temperature,
                    Humidity = er.Humidity,
                    CityTemperature = er.CityTemperature,
                    CityHumidity = er.CityHumidity,
                    CityWeatherCondition = er.CityWeatherCondition,
                    IsNight = er.ExtraData != null && er.ExtraData.Contains("\"is_night\": true"), // Ejemplo de como extraer un bool
                    RecordedAt = ToColombiaTime(er.RecordedAtServer)
                })
                .ToListAsync();

            return Result.Success(new PagedResultDto<SensorDataDisplayDto>
            {
                Items = new List<SensorDataDisplayDto>(),
                PageNumber = filters.PageNumber,
                PageSize = filters.PageSize,
                TotalCount = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo datos de sensores con filtros {@Filters}.", filters);
            return Result.Failure<PagedResultDto<SensorDataDisplayDto>>($"Error interno al obtener datos de sensores: {ex.Message}");
        }
    }

    public async Task<Result<PagedResultDto<ThermalCaptureSummaryDto>>> GetThermalCapturesAsync(DataQueryFilters filters)
    {
        _logger.LogInformation("Obteniendo capturas térmicas con filtros: {@Filters}", filters);
        try
        {
            var query = _context.ThermalCaptures.AsNoTracking();

            // Aplicar filtros
            if (filters.DeviceId.HasValue)
                query = query.Where(tc => tc.DeviceId == filters.DeviceId.Value);
            if (filters.PlantId.HasValue)
                query = query.Where(tc => tc.PlantId == filters.PlantId.Value);
            if (filters.CropId.HasValue)
                query = query.Where(tc => tc.Device.CropId == filters.CropId.Value);
            
            query = query.ApplyDateFilters(filters, tc => tc.RecordedAtServer);

            var totalCount = await query.CountAsync();
            if (totalCount == 0)
            {
                return Result.Success(new PagedResultDto<ThermalCaptureSummaryDto>
                {
                    Items = new List<ThermalCaptureSummaryDto>(),
                    PageNumber = filters.PageNumber,
                    PageSize = filters.PageSize,
                    TotalCount = 0
                });
            }

            var captures = await query
                .OrderByDescending(tc => tc.RecordedAtServer)
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToListAsync(); // Traemos los datos completos para procesar el JSON en memoria

            // Mapeo a DTO, incluyendo deserialización del JSON
            var summaries = captures.Select(m =>
            {
                var thermalStats = DeserializeThermalStats(m.ThermalDataStats, m.Id);
                return new ThermalCaptureSummaryDto
                {
                    Id = m.Id,
                    DeviceId = m.DeviceId,
                    DeviceName = m.Device?.Name ?? m.DeviceId.ToString(), // Asume que Device fue cargado (necesitará .Include si no se proyecta)
                    PlantName = m.Plant?.Name ?? "N/A",
                    MaxTemp = thermalStats?.Max_Temp ?? 0,
                    MinTemp = thermalStats?.Min_Temp ?? 0,
                    AvgTemp = thermalStats?.Avg_Temp ?? 0,
                    RgbImagePath = m.RgbImagePath,
                    RecordedAt = ToColombiaTime(m.RecordedAtServer)
                };
            }).ToList();

            // NOTA: El mapeo anterior es ineficiente porque carga entidades enteras. Una proyección directa es mejor.
            // Versión optimizada con proyección:
             var optimizedSummaries = await query
                .OrderByDescending(tc => tc.RecordedAtServer)
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(tc => new 
                {
                    Capture = tc,
                    DeviceName = tc.Device.Name,
                    PlantName = tc.Plant != null ? tc.Plant.Name : "N/A"
                })
                .ToListAsync();

            var finalSummaries = optimizedSummaries.Select(res => {
                var thermalStats = DeserializeThermalStats(res.Capture.ThermalDataStats, res.Capture.Id);
                 return new ThermalCaptureSummaryDto
                    {
                        Id = res.Capture.Id,
                        DeviceId = res.Capture.DeviceId,
                        DeviceName = res.DeviceName,
                        PlantName = res.PlantName,
                        MaxTemp = thermalStats?.Max_Temp ?? 0,
                        MinTemp = thermalStats?.Min_Temp ?? 0,
                        AvgTemp = thermalStats?.Avg_Temp ?? 0,
                        RgbImagePath = res.Capture.RgbImagePath,
                        RecordedAt = ToColombiaTime(res.Capture.RecordedAtServer)
                    };
            }).ToList();


            return Result.Success(new PagedResultDto<ThermalCaptureSummaryDto>
            {
                Items = new List<ThermalCaptureSummaryDto>(),
                PageNumber = filters.PageNumber,
                PageSize = filters.PageSize,
                TotalCount = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo capturas térmicas con filtros {@Filters}.", filters);
            return Result.Failure<PagedResultDto<ThermalCaptureSummaryDto>>($"Error interno al obtener capturas térmicas: {ex.Message}");
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
                    CropName = tc.Plant != null ? tc.Plant.Crop.Name : (tc.Device.Crop.Name ?? "N/A")
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
                RecordedAt = ToColombiaTime(result.Capture.RecordedAtServer),
                Temperatures = thermalStats?.Temperatures,
                ThermalDataJson = result.Capture.ThermalDataStats,
                ThermalImageWidth = 32, // Valor estático según requerimiento
                ThermalImageHeight = 24  // Valor estático según requerimiento
            };

            return Result.Success<ThermalCaptureDetailsDto?>(detailsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo detalles de captura térmica ID: {CaptureId}", captureId);
            return Result.Failure<ThermalCaptureDetailsDto?>($"Error interno al obtener detalles de captura: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<SensorDataDisplayDto>>> GetAmbientDataForDashboardAsync(TimeSpan duration, int? cropId, int? plantId, int? deviceId)
    {
        _logger.LogInformation("Obteniendo datos ambientales para dashboard. Duración: {Duration}, CropId: {CropId}, PlantId: {PlantId}, DeviceId: {DeviceId}",
            duration, cropId, plantId, deviceId);
        try
        {
            var since = DateTime.UtcNow.Subtract(duration);
            var query = _context.EnvironmentalReadings.AsNoTracking()
                .Where(er => er.RecordedAtServer >= since);

            // Aplicar filtros jerárquicos
            if (deviceId.HasValue)
                query = query.Where(er => er.DeviceId == deviceId.Value);
            else if (plantId.HasValue)
                query = query.Where(er => er.PlantId == plantId.Value);
            else if (cropId.HasValue)
                query = query.Where(er => er.Device.CropId == cropId.Value);

            var data = await query
                .OrderBy(er => er.RecordedAtServer)
                .Select(er => new SensorDataDisplayDto
                {
                    DeviceId = er.DeviceId,
                    DeviceName = er.Device.Name,
                    Light = GetLightValueFromJson(er.ExtraData),
                    Temperature = er.Temperature,
                    Humidity = er.Humidity,
                    CityTemperature = er.CityTemperature,
                    CityHumidity = er.CityHumidity,
                    IsNight = er.ExtraData != null && er.ExtraData.Contains("\"is_night\": true"),
                    RecordedAt = ToColombiaTime(er.RecordedAtServer)
                }).ToListAsync();
            
            _logger.LogInformation("Datos ambientales para dashboard recuperados: {Count} puntos.", data.Count);
            return Result.Success<IEnumerable<SensorDataDisplayDto>>(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo datos ambientales para el dashboard.");
            return Result.Failure<IEnumerable<SensorDataDisplayDto>>($"Error interno al obtener datos para el dashboard: {ex.Message}");
        }
    }

    public async Task<Result<ThermalStatsDto>> GetThermalStatsForDashboardAsync(TimeSpan duration, int? cropId, int? plantId, int? deviceId)
    {
        _logger.LogInformation("Obteniendo estadísticas térmicas para dashboard. Duración: {Duration}, etc.", duration);
        try
        {
            var since = DateTime.UtcNow.Subtract(duration);
            var query = _context.ThermalCaptures.AsNoTracking()
                .Where(tc => tc.RecordedAtServer >= since);

            // Filtros jerárquicos
            if (deviceId.HasValue)
                query = query.Where(tc => tc.DeviceId == deviceId.Value);
            else if (plantId.HasValue)
                query = query.Where(tc => tc.PlantId == plantId.Value);
            else if (cropId.HasValue)
                query = query.Where(tc => tc.Device.CropId == cropId.Value);

            var allCapturesInRange = await query.Select(tc => new { tc.Id, tc.RecordedAtServer, tc.ThermalDataStats }).ToListAsync();

            if (!allCapturesInRange.Any())
            {
                _logger.LogInformation("No hay datos térmicos recientes para los filtros del dashboard.");
                return Result.Success(new ThermalStatsDto());
            }

            var thermalStatsList = new List<ThermalDataDto>();
            foreach (var model in allCapturesInRange)
            {
                var stats = DeserializeThermalStats(model.ThermalDataStats, model.Id);
                if (stats != null) thermalStatsList.Add(stats);
            }

            if (!thermalStatsList.Any())
            {
                 _logger.LogInformation("No se pudieron deserializar datos térmicos válidos para el dashboard.");
                return Result.Success(new ThermalStatsDto());
            }
            
            var latestCapture = allCapturesInRange.OrderByDescending(x => x.RecordedAtServer).First();
            var latestStats = DeserializeThermalStats(latestCapture.ThermalDataStats, latestCapture.Id);

            var dashboardStats = new ThermalStatsDto
            {
                AverageMaxTemp24h = thermalStatsList.Average(s => s.Max_Temp),
                AverageMinTemp24h = thermalStatsList.Average(s => s.Min_Temp),
                AverageAvgTemp24h = thermalStatsList.Average(s => s.Avg_Temp),
                LatestMaxTemp = latestStats?.Max_Temp ?? 0,
                LatestMinTemp = latestStats?.Min_Temp ?? 0,
                LatestAvgTemp = latestStats?.Avg_Temp ?? 0,
                LatestThermalReadingTimestamp = ToColombiaTime(latestCapture.RecordedAtServer)
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
            _logger.LogInformation("Conteo de dispositivos activos (CropId: {CropId}, PlantId: {PlantId}): {Count}", cropId, plantId, count);
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
            if (deviceId.HasValue)
                query = query.Where(er => er.DeviceId == deviceId.Value);
            else if (plantId.HasValue)
                query = query.Where(er => er.PlantId == plantId.Value);
            else if (cropId.HasValue)
                query = query.Where(er => er.Device.CropId == cropId.Value);
            
            var result = await query
                .OrderByDescending(er => er.RecordedAtServer)
                .Select(er => new SensorDataDisplayDto // Proyectar directamente a DTO
                {
                    Id = er.Id,
                    DeviceId = er.DeviceId,
                    DeviceName = er.Device.Name,
                    PlantName = er.Plant != null ? er.Plant.Name : "N/A",
                    CropName = er.Device.Crop.Name,
                    Light = GetLightValueFromJson(er.ExtraData),
                    Temperature = er.Temperature,
                    Humidity = er.Humidity,
                    CityTemperature = er.CityTemperature,
                    CityHumidity = er.CityHumidity,
                    CityWeatherCondition = er.CityWeatherCondition,
                    IsNight = er.ExtraData != null && er.ExtraData.Contains("\"is_night\": true"),
                    RecordedAt = ToColombiaTime(er.RecordedAtServer)
                })
                .FirstOrDefaultAsync(); // Tomar solo el primero

            if (result == null)
            {
                _logger.LogInformation("No se encontró última lectura ambiental para los filtros aplicados.");
            }
            
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo última lectura ambiental.");
            return Result.Failure<SensorDataDisplayDto?>($"Error interno al obtener última lectura ambiental: {ex.Message}");
        }
    }
}

// Clase auxiliar para construir predicados dinámicos. Útil si se quiere refactorizar los filtros.
public static class PredicateBuilder
{
    public static System.Linq.Expressions.Expression<Func<T, bool>> New<T>(bool defaultExpression)
    {
        return f => defaultExpression;
    }

    public static System.Linq.Expressions.Expression<Func<T, bool>> Or<T>(this System.Linq.Expressions.Expression<Func<T, bool>> expr1, System.Linq.Expressions.Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = System.Linq.Expressions.Expression.Invoke(expr2, expr1.Parameters.Cast<System.Linq.Expressions.Expression>());
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
    }

    public static System.Linq.Expressions.Expression<Func<T, bool>> And<T>(this System.Linq.Expressions.Expression<Func<T, bool>> expr1, System.Linq.Expressions.Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = System.Linq.Expressions.Expression.Invoke(expr2, expr1.Parameters.Cast<System.Linq.Expressions.Expression>());
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
    }
}