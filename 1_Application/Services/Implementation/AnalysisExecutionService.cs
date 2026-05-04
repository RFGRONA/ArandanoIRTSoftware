using System.Text.Json;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ArandanoIRT.Web._1_Application.Services.Contracts.IAnalysisExecutionService;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

/// <summary>
///     Implementación del servicio que encapsula la lógica para ejecutar los cálculos de análisis de estrés hídrico.
/// </summary>
public class AnalysisExecutionService : IAnalysisExecutionService
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, System.Threading.SemaphoreSlim> _plantLocks = new();

    private readonly ApplicationDbContext _context;
    private readonly IDataQueryService _dataQueryService;
    private readonly ILogger<AnalysisExecutionService> _logger;
    private readonly IConditionPredictor _predictor;
    private readonly ICropService _cropService;
    private readonly IAlertTriggerService _alertTriggerService;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="AnalysisExecutionService" />.
    /// </summary>
    public AnalysisExecutionService(ApplicationDbContext context, ILogger<AnalysisExecutionService> logger,
        IConditionPredictor predictor, IDataQueryService dataQueryService,
        ICropService cropService, IAlertTriggerService alertTriggerService)
    {
        _context = context;
        _logger = logger;
        _predictor = predictor;
        _dataQueryService = dataQueryService;
        _cropService = cropService;
        _alertTriggerService = alertTriggerService;
    }

    /// <inheritdoc />
    public async Task<Result<AnalysisResult>> CalculateCwsiAsync(CwsiCalculationInput input)
    {
        var lightValue = GetLightValueFromJson(input.EnvironmentalReading.ExtraData);
        var vpdValue = CalculateVpdKpa(input.EnvironmentalReading.Temperature,
            input.EnvironmentalReading.Humidity);

        if (!vpdValue.HasValue)
            return Result.Failure<AnalysisResult>("No se pudo calcular el VPD a partir de los datos ambientales.");

        var modelInput = new[]
        {
            input.EnvironmentalReading.Temperature,
            input.EnvironmentalReading.Humidity,
            lightValue ?? 0.0f,
            vpdValue.Value
        };
        var prediction = await _predictor.IsConditionSuitableAsync(modelInput);

        if (prediction == 0)
            return Result.Failure<AnalysisResult>("Condiciones ambientales no aptas según el modelo.");

        var tCanopy = GetCanopyTemperature(input.MonitoredPlantCapture, input.MonitoredPlant.ThermalMaskData);

        if (!tCanopy.HasValue)
            return Result.Failure<AnalysisResult>("No se pudo determinar T.Canopia a partir de las capturas térmicas.");

        // Clipping: Verificar si la temperatura del canopio es válida
        var p = input.Parameters;
        if (tCanopy.Value < p.MinValidCanopyTemp || tCanopy.Value > p.MaxValidCanopyTemp)
            return Result.Failure<AnalysisResult>($"Temperatura de canopia ({tCanopy.Value}°C) fuera del rango válido de ({p.MinValidCanopyTemp} - {p.MaxValidCanopyTemp}).");

        // Modelamiento Empírico de Líneas Base
        var ll = (float)((p.EmpiricalM * vpdValue.Value) + p.EmpiricalC);
        var ul = (float)p.EmpiricalUl;

        var tDiff = tCanopy.Value - input.EnvironmentalReading.Temperature;

        if (ul - ll <= 0.01)
            return Result.Failure<AnalysisResult>("La diferencia entre UL y LL empíricos es muy pequeña matemáticamente.");

        var cwsi = (tDiff - ll) / (ul - ll);
        cwsi = Math.Clamp(cwsi, 0, 1);

        var result = new AnalysisResult
        {
            PlantId = input.MonitoredPlant.Id,
            RecordedAt = input.EnvironmentalReading.RecordedAtServer,
            CwsiValue = (float)cwsi,
            CanopyTemperature = tCanopy.Value,
            AmbientTemperature = input.EnvironmentalReading.Temperature,
            Vpd = vpdValue.Value,
            BaselineLL = ll,
            BaselineUL = ul,
            Status = PlantStatus.UNKNOWN
        };

        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task ExecuteCatchUpForPlantAsync(int plantId)
    {
        var plantLock = _plantLocks.GetOrAdd(plantId, _ => new System.Threading.SemaphoreSlim(1, 1));

        if (!await plantLock.WaitAsync(0))
        {
            _logger.LogInformation("El catch-up para la planta {PlantId} ya está en curso. Evitando ejecución simultánea.", plantId);
            return;
        }

        try
        {
            _logger.LogInformation("Iniciando análisis de catch-up para la planta {PlantId}", plantId);

            var monitoredPlant = await _context.Plants.AsNoTracking().FirstOrDefaultAsync(p => p.Id == plantId);
            if (monitoredPlant == null) return;

            var lastAnalysisDate = await _context.AnalysisResults
                .Where(ar => ar.PlantId == plantId)
                .OrderByDescending(ar => ar.RecordedAt)
                .Select(ar => (DateTime?)ar.RecordedAt)
                .FirstOrDefaultAsync();

            var environmentalData = await _context.EnvironmentalReadings
                .Where(er => er.PlantId == plantId && er.RecordedAtServer > (lastAnalysisDate ?? DateTime.MinValue))
                .OrderBy(er => er.RecordedAtServer)
                .ToListAsync();

            if (!environmentalData.Any())
            {
                _logger.LogInformation("No hay nuevos datos crudos para analizar en el catch-up de la planta {PlantId}",
                    plantId);
                return;
            }

            var dateRangeStart = environmentalData.First().RecordedAtServer.AddMinutes(-5); // Ampliar rango de búsqueda
            var dateRangeEnd = environmentalData.Last().RecordedAtServer.AddMinutes(5); // Ampliar rango de búsqueda

            var monitoredThermals = await _context.ThermalCaptures
                .Where(tc =>
                    tc.PlantId == plantId && tc.RecordedAtServer >= dateRangeStart && tc.RecordedAtServer <= dateRangeEnd)
                .OrderBy(tc => tc.RecordedAtServer)
                .ToListAsync();

            var parametersResult = await _cropService.GetAnalysisParametersAsync(monitoredPlant.CropId);
            if (parametersResult.IsFailure)
            {
                _logger.LogWarning("No se encontraron parámetros de análisis para el cultivo {CropId}", monitoredPlant.CropId);
                return;
            }
            var parameters = parametersResult.Value.AnalysisParameters;

            var newAnalysisResults = new List<AnalysisResult>();
            var currentStatus = monitoredPlant.Status;

            foreach (var reading in environmentalData)
            {
                var hour = reading.RecordedAtServer.ToColombiaTime().Hour;
                if (hour < parameters.AnalysisWindowStartHour || hour > parameters.AnalysisWindowEndHour)
                {
                    continue;
                }

                // Buscar la captura térmica más cercana para la planta monitoreada
                var monitoredCapture = monitoredThermals
                    .Where(tc => Math.Abs((tc.RecordedAtServer - reading.RecordedAtServer).TotalMinutes) <= 5)
                    .MinBy(tc => Math.Abs((tc.RecordedAtServer - reading.RecordedAtServer).TotalMinutes));

                if (monitoredCapture == null)
                {
                    _logger.LogWarning(
                        "No se encontró captura térmica cercana para la lectura ambiental en {RecordedAtServer} de la planta {PlantId}",
                        reading.RecordedAtServer, plantId);
                    continue;
                }

                var input = new CwsiCalculationInput(reading, monitoredCapture, monitoredPlant, parameters);
                var calculationResult = await CalculateCwsiAsync(input);

                if (calculationResult.IsSuccess)
                {
                    var analysis = calculationResult.Value;
                    var cwsiValueDouble = (double)(analysis.CwsiValue ?? 0f);
                    var newStatus = DetermineStatus(cwsiValueDouble, parameters, currentStatus);
                    analysis.Status = newStatus;

                    newAnalysisResults.Add(analysis);
                    currentStatus = newStatus; // actualizamos el estado actual circulante
                }
                else
                {
                    _logger.LogWarning("Fallo el calculo de CWSI para la lectura ambiental en {RecordedAtServer}: {Error}",
                        reading.RecordedAtServer, calculationResult.ErrorMessage);
                }
            }

            if (newAnalysisResults.Any())
            {
                await _context.AnalysisResults.AddRangeAsync(newAnalysisResults);

                // Actualizar la planta y generar alertas si el último registro marca un cambio
                var lastResult = newAnalysisResults.Last();
                var plantToUpdate = await _context.Plants.FindAsync(plantId);

                if (plantToUpdate != null && plantToUpdate.Status != lastResult.Status)
                {
                    var oldStatus = plantToUpdate.Status;
                    plantToUpdate.Status = lastResult.Status;
                    plantToUpdate.UpdatedAt = DateTime.UtcNow;

                    var historyRecord = new PlantStatusHistory
                    {
                        PlantId = plantId,
                        Status = lastResult.Status,
                        Observation = $"Cambio de estado en Catch-up automático por el sistema basado en un valor CWSI de {lastResult.CwsiValue:F2}.",
                        UserId = null,
                        ChangedAt = DateTime.UtcNow
                    };
                    _context.PlantStatusHistories.Add(historyRecord);

                    await _alertTriggerService.TriggerStressAlertAsync(
                        plantId,
                        plantToUpdate.Name,
                        lastResult.Status,
                        oldStatus,
                        lastResult.CwsiValue ?? 0f
                    );
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Catch-up completado. Se generaron {Count} nuevos registros para la planta {PlantId}",
                    newAnalysisResults.Count, plantId);
            }
            else
            {
                _logger.LogInformation("No se generaron nuevos resultados de análisis para la planta {PlantId}", plantId);
            }
        }
        finally
        {
            plantLock.Release();
        }
    }

    private PlantStatus DetermineStatus(double cwsi, ArandanoIRT.Web._0_Domain.Entities.AnalysisParameters parameters, PlantStatus previousStatus)
    {
        PlantStatus newStatus;
        if (cwsi > parameters.CwsiThresholdCritical)
            newStatus = PlantStatus.SEVERE_STRESS;
        else if (cwsi > parameters.CwsiThresholdIncipient)
            newStatus = PlantStatus.MILD_STRESS;
        else
            newStatus = PlantStatus.OPTIMAL;

        // Lógica de recuperación
        var wasStressed = previousStatus == PlantStatus.MILD_STRESS || previousStatus == PlantStatus.SEVERE_STRESS;
        if (wasStressed && newStatus == PlantStatus.OPTIMAL) return PlantStatus.RECOVERING;

        if (previousStatus == PlantStatus.RECOVERING && newStatus == PlantStatus.OPTIMAL) return PlantStatus.OPTIMAL;

        return newStatus;
    }

    /// <summary>
    ///     Extrae la temperatura de la canopia desde una captura térmica.
    ///     Prioriza el cálculo usando una máscara térmica si está disponible; de lo contrario, usa el promedio general.
    /// </summary>
    /// <param name="capture">La entidad de la captura térmica.</param>
    /// <param name="maskJson">La cadena JSON con las coordenadas de la máscara térmica.</param>
    /// <returns>La temperatura promedio de la canopia o null si no se puede calcular.</returns>
    private float? GetCanopyTemperature(ThermalCapture? capture, string? maskJson)
    {
        if (capture == null || string.IsNullOrWhiteSpace(capture.ThermalDataStats)) return null;

        try
        {
            var stats = JsonSerializer.Deserialize<ThermalDataDto>(capture.ThermalDataStats,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (stats == null) return null;

            if (!string.IsNullOrWhiteSpace(maskJson) && stats.Temperatures != null)
            {
                var maskContainer = JsonSerializer.Deserialize<MaskContainer>(maskJson);
                var mask = maskContainer?.thermal_mask;
                if (mask?.coordinates != null && mask.coordinates.Any())
                {
                    var maskedTemperatures = new List<float>();
                    foreach (var coord in mask.coordinates)
                        if (coord.y >= 0 && coord.y < 24 && coord.x >= 0 && coord.x < 32)
                        {
                            var index = coord.y * 32 + coord.x;
                            if (index < stats.Temperatures.Count)
                                maskedTemperatures.Add(stats.Temperatures[index].Value);
                        }

                    if (maskedTemperatures.Any()) return maskedTemperatures.Average();
                }
            }

            return stats.Avg_Temp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar máscara térmica para la captura {CaptureId}", capture.Id);
            return null;
        }
    }

    /// <summary>
    ///     DTO interno para deserializar el contenedor principal del JSON de la máscara.
    /// </summary>
    private class MaskContainer
    {
        public MaskData? thermal_mask { get; set; }
    }

    /// <summary>
    ///     DTO interno para deserializar los datos de la máscara.
    /// </summary>
    private class MaskData
    {
        public List<Coord>? coordinates { get; set; }
    }

    /// <summary>
    ///     DTO interno para deserializar las coordenadas x, y.
    /// </summary>
    private class Coord
    {
        public int x { get; set; }
        public int y { get; set; }
    }

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
            _logger.LogWarning(ex, "No se pudo parsear el JSON de ExtraData para obtener luz.");
        }

        return null;
    }

    private float? CalculateVpdKpa(float temperature, float humidity)
    {
        // VPD (kPa) = e_s - e_a
        // e_s = 0.6108 * exp(17.27 * T / (T + 237.3))
        // e_a = e_s * (HR / 100)
        double es = 0.6108 * Math.Exp(17.27 * temperature / (temperature + 237.3));
        double ea = es * (humidity / 100.0);
        return (float)(es - ea);
    }
}