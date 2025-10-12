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
    /// <summary>
    ///     Contiene los umbrales de la línea base no estresada (T_canopia - T_ambiente) para cada hora del día.
    ///     Estos valores son fundamentales para calcular la temperatura de referencia seca (T_dry).
    /// </summary>
    private static readonly Dictionary<int, double> HourlyStressThresholds = new()
    {
        { 0, 0.71 }, { 1, 0.51 }, { 2, 0.55 }, { 3, 0.63 }, { 4, 0.67 },
        { 5, 0.76 }, { 6, 0.73 }, { 7, 0.68 }, { 8, 0.14 }, { 9, -0.95 },
        { 10, -1.37 }, { 11, -1.21 }, { 12, -0.64 }, { 13, -1.07 }, { 14, -1.00 },
        { 15, -0.87 }, { 16, -0.29 }, { 17, 0.15 }, { 18, 0.41 }, { 19, 0.59 },
        { 20, 0.56 }, { 21, 0.62 }, { 22, 0.65 }, { 23, 0.64 }
    };

    private readonly ApplicationDbContext _context;
    private readonly IDataQueryService _dataQueryService;
    private readonly ILogger<AnalysisExecutionService> _logger;
    private readonly IConditionPredictor _predictor;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="AnalysisExecutionService" />.
    /// </summary>
    public AnalysisExecutionService(ApplicationDbContext context, ILogger<AnalysisExecutionService> logger,
        IConditionPredictor predictor, IDataQueryService dataQueryService)
    {
        _context = context;
        _logger = logger;
        _predictor = predictor;
        _dataQueryService = dataQueryService;
    }

    /// <inheritdoc />
    public async Task<Result<AnalysisResult>> CalculateCwsiAsync(CwsiCalculationInput input)
    {
        var lightValue = _dataQueryService.GetLightValueFromJson(input.EnvironmentalReading.ExtraData);
        var vpdValue = _dataQueryService.CalculateVpdKpa(input.EnvironmentalReading.Temperature,
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
        var tWet = GetCanopyTemperature(input.ControlPlantCapture, input.ControlPlant.ThermalMaskData);

        if (!tCanopy.HasValue || !tWet.HasValue)
            return Result.Failure<AnalysisResult>(
                "No se pudo determinar T.Canopia o T.Húmeda a partir de las capturas térmicas.");

        var hour = input.EnvironmentalReading.RecordedAtServer.ToColombiaTime().Hour;
        var threshold = HourlyStressThresholds[hour];
        var tDry = tWet.Value + threshold;

        if (tDry - tWet.Value <= 0.1)
            return Result.Failure<AnalysisResult>(
                "La diferencia entre T_dry y T_wet es muy pequeña para un cálculo fiable.");

        var cwsi = (tCanopy.Value - tWet.Value) / (tDry - tWet.Value);
        cwsi = Math.Clamp(cwsi, 0, 1);

        var result = new AnalysisResult
        {
            PlantId = input.MonitoredPlant.Id,
            RecordedAt = input.EnvironmentalReading.RecordedAtServer,
            CwsiValue = (float)cwsi,
            CanopyTemperature = tCanopy.Value,
            AmbientTemperature = input.EnvironmentalReading.Temperature,
            Vpd = vpdValue.Value,
            BaselineTwet = tWet.Value,
            BaselineTdry = (float)tDry,
            Status = PlantStatus.UNKNOWN
        };

        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task ExecuteCatchUpForPlantAsync(int plantId)
    {
        _logger.LogInformation("Iniciando análisis de catch-up para la planta {PlantId}", plantId);

        var monitoredPlant = await _context.Plants.AsNoTracking().FirstOrDefaultAsync(p => p.Id == plantId);
        if (monitoredPlant == null) return;

        var controlPlant = await _context.Plants.AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.CropId == monitoredPlant.CropId && p.ExperimentalGroup == ExperimentalGroupType.CONTROL);

        if (controlPlant == null)
        {
            _logger.LogWarning("No se encontró planta de control para el cultivo {CropId}. Cancelando catch-up.",
                monitoredPlant.CropId);
            return;
        }

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

        var dateRangeStart = environmentalData.First().RecordedAtServer;
        var dateRangeEnd = environmentalData.Last().RecordedAtServer;

        var monitoredThermals = await _context.ThermalCaptures
            .Where(tc =>
                tc.PlantId == plantId && tc.RecordedAtServer >= dateRangeStart && tc.RecordedAtServer <= dateRangeEnd)
            .ToDictionaryAsync(tc => tc.RecordedAtServer, tc => tc);

        var controlThermals = await _context.ThermalCaptures
            .Where(tc => tc.PlantId == controlPlant.Id && tc.RecordedAtServer >= dateRangeStart &&
                         tc.RecordedAtServer <= dateRangeEnd)
            .ToDictionaryAsync(tc => tc.RecordedAtServer, tc => tc);

        var newAnalysisResults = new List<AnalysisResult>();
        foreach (var reading in environmentalData)
        {
            if (!monitoredThermals.TryGetValue(reading.RecordedAtServer, out var monitoredCapture) ||
                !controlThermals.TryGetValue(reading.RecordedAtServer, out var controlCapture))
                continue;

            var input = new CwsiCalculationInput(reading, monitoredCapture, controlCapture, monitoredPlant,
                controlPlant);
            var calculationResult = await CalculateCwsiAsync(input);

            if (calculationResult.IsSuccess) newAnalysisResults.Add(calculationResult.Value);
        }

        if (newAnalysisResults.Any())
        {
            await _context.AnalysisResults.AddRangeAsync(newAnalysisResults);
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Catch-up completado. Se generaron {Count} nuevos registros para la planta {PlantId}",
                newAnalysisResults.Count, plantId);
        }
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
}