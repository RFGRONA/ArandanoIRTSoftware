using System.Linq.Expressions;
using System.Text.Json;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;
using ArandanoIRT.Web._1_Application.DTOs.Common;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
using ArandanoIRT.Web._1_Application.DTOs.SensorData;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._3_Presentation.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class DataQueryService : IDataQueryService
{
    private readonly TimeZoneInfo _colombiaZone;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataQueryService> _logger;

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
            _logger.LogCritical(tzex,
                "La zona horaria 'America/Bogota' no se encontró en el sistema. Las conversiones de fecha pueden ser incorrectas.");
            // Fallback a la zona UTC si no se encuentra la de Colombia para evitar que la aplicación falle al iniciar.
            _colombiaZone = TimeZoneInfo.Utc;
        }
    }

    public async Task<Result<PagedResultDto<SensorDataDisplayDto>>> GetSensorDataAsync(DataQueryFilters filters)
    {
        _logger.LogInformation("Obteniendo datos de sensores con filtros: {@Filters}", filters);
        try
        {
            var query = _context.EnvironmentalReadings.AsNoTracking();

            // Filtros
            if (filters.DeviceId.HasValue) query = query.Where(er => er.DeviceId == filters.DeviceId.Value);
            if (filters.PlantId.HasValue) query = query.Where(er => er.PlantId == filters.PlantId.Value);
            if (filters.CropId.HasValue) query = query.Where(er => er.Device.CropId == filters.CropId.Value);
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

            // 1. Traer datos crudos
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
                    CropName = er.Device.Crop.Name,
                    er.Temperature,
                    er.Humidity,
                    er.ExtraData,
                    er.CityTemperature,
                    er.CityHumidity,
                    er.CityWeatherCondition,
                    er.RecordedAtServer
                })
                .ToListAsync();

            // 2. Transformar en memoria
            var finalData = rawData.Select(er => new SensorDataDisplayDto
            {
                Id = er.Id,
                DeviceId = er.DeviceId,
                DeviceName = er.DeviceName,
                PlantName = er.PlantName,
                CropName = er.CropName,
                Light = GetLightValueFromJson(er.ExtraData),
                Temperature = er.Temperature,
                Humidity = er.Humidity,
                CityTemperature = er.CityTemperature,
                CityHumidity = er.CityHumidity,
                CityWeatherCondition = er.CityWeatherCondition,
                IsNight = er.ExtraData != null && er.ExtraData.Contains("\"is_night\": true"),
                RecordedAt = er.RecordedAtServer.ToColombiaTime()
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
            var query = _context.ThermalCaptures.AsNoTracking();

            // Aplicar filtros (esta lógica no cambia)
            if (filters.DeviceId.HasValue) query = query.Where(tc => tc.DeviceId == filters.DeviceId.Value);
            if (filters.PlantId.HasValue) query = query.Where(tc => tc.PlantId == filters.PlantId.Value);
            if (filters.CropId.HasValue) query = query.Where(tc => tc.Device.CropId == filters.CropId.Value);
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

            // ================== INICIO DE LA CORRECCIÓN ==================

            // 1. Traer datos crudos de la BD, incluyendo el JSON como texto.
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
                    tc.ThermalDataStats, // Se trae el JSON como string
                    tc.RgbImagePath,
                    tc.RecordedAtServer
                })
                .ToListAsync();

            // 2. Transformar los datos en memoria. Ahora sí podemos llamar a los métodos de C#.
            var finalData = rawData.Select(m =>
            {
                var thermalStats = DeserializeThermalStats(m.ThermalDataStats, m.Id); // Llamada segura
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
                    RecordedAt = m.RecordedAtServer.ToColombiaTime() // Llamada segura
                };
            }).ToList();

            // =================== FIN DE LA CORRECCIÓN ====================

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
                RecordedAt = result.Capture.RecordedAtServer.ToColombiaTime(),
                Temperatures = thermalStats?.Temperatures,
                ThermalDataJson = result.Capture.ThermalDataStats,
                ThermalImageWidth = 32, // Valor estático según requerimiento
                ThermalImageHeight = 24 // Valor estático según requerimiento
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
        int? cropId, int? plantId, int? deviceId)
    {
        _logger.LogInformation("Obteniendo datos ambientales para dashboard. Duración: {Duration}, etc.", duration);
        try
        {
            var since = DateTime.UtcNow.Subtract(duration);
            var query = _context.EnvironmentalReadings.AsNoTracking().Where(er => er.RecordedAtServer >= since);

            if (deviceId.HasValue) query = query.Where(er => er.DeviceId == deviceId.Value);
            else if (plantId.HasValue) query = query.Where(er => er.PlantId == plantId.Value);
            else if (cropId.HasValue) query = query.Where(er => er.Device.CropId == cropId.Value);

            // 1. Traer datos crudos
            var rawData = await query
                .OrderBy(er => er.RecordedAtServer)
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
                Light = GetLightValueFromJson(er.ExtraData), // Llamada segura
                Temperature = er.Temperature,
                Humidity = er.Humidity,
                CityTemperature = er.CityTemperature,
                CityHumidity = er.CityHumidity,
                IsNight = er.ExtraData != null && er.ExtraData.Contains("\"is_night\": true"),
                RecordedAt = er.RecordedAtServer.ToColombiaTime() // Llamada segura
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
        int? plantId, int? deviceId)
    {
        _logger.LogInformation("Obteniendo estadísticas térmicas para dashboard. Duración: {Duration}", duration);
        try
        {
            var since = DateTime.UtcNow.Subtract(duration);
            var query = _context.ThermalCaptures.AsNoTracking().Where(tc => tc.RecordedAtServer >= since);

            // Aplicar filtros (esta lógica no cambia)
            if (deviceId.HasValue) query = query.Where(tc => tc.DeviceId == deviceId.Value);
            else if (plantId.HasValue) query = query.Where(tc => tc.PlantId == plantId.Value);
            else if (cropId.HasValue) query = query.Where(tc => tc.Device.CropId == cropId.Value);

            // ================== INICIO DE LA CORRECCIÓN ==================

            // 1. Traer solo el JSON y la fecha, sin procesar nada.
            var rawCaptures = await query.Select(tc => new { tc.Id, tc.RecordedAtServer, tc.ThermalDataStats })
                .ToListAsync();

            if (!rawCaptures.Any())
            {
                _logger.LogInformation("No hay datos térmicos recientes para los filtros del dashboard.");
                return Result.Success(new ThermalStatsDto()); // Devolver un objeto vacío pero exitoso.
            }

            // 2. Deserializar toda la lista en memoria.
            var thermalStatsList = new List<ThermalDataDto>();
            foreach (var model in rawCaptures)
            {
                var stats = DeserializeThermalStats(model.ThermalDataStats, model.Id); // Llamada segura
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
            var latestStats = thermalStatsList.LastOrDefault(); // El último de la lista ya deserializada

            var dashboardStats = new ThermalStatsDto
            {
                AverageMaxTemp24h = thermalStatsList.Average(s => s.Max_Temp),
                AverageMinTemp24h = thermalStatsList.Average(s => s.Min_Temp),
                AverageAvgTemp24h = thermalStatsList.Average(s => s.Avg_Temp),
                LatestMaxTemp = latestStats?.Max_Temp,
                LatestMinTemp = latestStats?.Min_Temp,
                LatestAvgTemp = latestStats?.Avg_Temp,
                LatestThermalReadingTimestamp = latestCapture.RecordedAtServer.ToColombiaTime() // Llamada segura
            };

            // =================== FIN DE LA CORRECCIÓN ====================

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
                RecordedAt = rawResult.RecordedAtServer.ToColombiaTime()
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
                .Where(tc => tc.PlantId == plantId && tc.RgbImagePath != null && EF.Functions.JsonExists(tc.ThermalDataStats, "temperatures"))
                .OrderByDescending(tc => tc.RecordedAtServer)
                .Select(tc => new { tc.ThermalDataStats, tc.RgbImagePath }) // Seleccionamos ambos campos
                .FirstOrDefaultAsync();

            if (latestCapture == null)
            {
                return Result.Success<(ThermalDataDto? Stats, string? ImagePath)>((null, null));
            }

            var thermalStats = DeserializeThermalStats(latestCapture.ThermalDataStats, 0);
            return Result.Success((thermalStats, latestCapture.RgbImagePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo la última captura para la máscara de la planta {PlantId}", plantId);
            return Result.Failure<(ThermalDataDto? Stats, string? ImagePath)>("Error interno al obtener datos de la captura.");
        }
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
                lightElement.TryGetSingle(out var lightValue)) return lightValue;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "No se pudo parsear el JSON de ExtraData para obtener 'light'. Contenido: {Json}",
                extraDataJson);
        }

        return null;
    }

    /// <summary>
    ///     Parsea de forma segura el campo de estadísticas térmicas (JSON).
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
}

// Clase auxiliar para construir predicados dinámicos. Útil si se quiere refactorizar los filtros.
public static class PredicateBuilder
{
    public static Expression<Func<T, bool>> New<T>(bool defaultExpression)
    {
        return f => defaultExpression;
    }

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
    }

    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
    }
}