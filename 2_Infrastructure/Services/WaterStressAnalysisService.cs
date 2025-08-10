using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

namespace ArandanoIRT.Web._2_Infrastructure.Services;

public class WaterStressAnalysisService : BackgroundService
{
    private readonly ILogger<WaterStressAnalysisService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundJobSettings _settings;

    public WaterStressAnalysisService(
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundJobSettings> settings,
        ILogger<WaterStressAnalysisService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_settings.AnalysisIntervalMinutes);
        using var timer = new PeriodicTimer(interval);
        _logger.LogInformation("Servicio de Análisis de Estrés Hídrico iniciado. Verificando cada {Minutes} minutos.", _settings.AnalysisIntervalMinutes);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cropService = scope.ServiceProvider.GetRequiredService<ICropService>();

            var crops = await dbContext.Crops.AsNoTracking().ToListAsync(stoppingToken);

            foreach (var crop in crops)
            {
                var parametersResult = await cropService.GetAnalysisParametersAsync(crop.Id);
                if (parametersResult.IsFailure) continue;

                var parameters = parametersResult.Value.AnalysisParameters;
                var nowUtc = DateTime.UtcNow;

                if (!nowUtc.IsWithinColombiaTimeWindow(parameters.AnalysisWindowStartHour, parameters.AnalysisWindowEndHour))
                {
                    continue; // No estamos en la ventana de análisis para este cultivo
                }

                _logger.LogInformation("Iniciando ciclo de análisis para el cultivo: {CropName}", crop.Name);
                await RunAnalysisCycleAsync(scope.ServiceProvider, crop, parameters, nowUtc, stoppingToken);
            }
        }
    }

    private async Task RunAnalysisCycleAsync(IServiceProvider services, Crop crop, AnalysisParameters parameters, DateTime nowUtc, CancellationToken token)
    {
        var dataQueryService = services.GetRequiredService<IDataQueryService>();
        var environmentalDataProvider = services.GetRequiredService<IEnvironmentalDataProvider>();
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var alertTriggerService = services.GetRequiredService<IAlertTriggerService>();

        // 1. Obtener plantas y validar la configuración del cultivo
        var plantsInCrop = await dbContext.Plants
            .Where(p => p.CropId == crop.Id)
            .ToListAsync(token);

        var hasControl = plantsInCrop.Any(p => p.ExperimentalGroup == ExperimentalGroupType.CONTROL);
        var hasStress = plantsInCrop.Any(p => p.ExperimentalGroup == ExperimentalGroupType.STRESS);
        var hasMonitored = plantsInCrop.Any(p => p.ExperimentalGroup == ExperimentalGroupType.MONITORED);

        if (!hasControl || !hasStress || !hasMonitored)
        {
            _logger.LogWarning("El cultivo {CropName} no puede ser analizado. Se requiere al menos una planta de tipo 'Control', 'Stress' y 'Monitored'.", crop.Name);
            return; // Detenemos el análisis para este cultivo
        }

        // 2. Obtener datos crudos
        var allPlantIds = plantsInCrop.Select(p => p.Id).ToList();
        var startTime = nowUtc.AddMinutes(-_settings.AnalysisIntervalMinutes);
        var rawDataResult = await dataQueryService.GetRawDataForAnalysisAsync(allPlantIds, startTime, nowUtc);
        if (rawDataResult.IsFailure || !rawDataResult.Value.Any()) return;

        var plantsData = rawDataResult.Value;

        // 3. Validar condiciones ambientales
        var referenceReading = plantsData.SelectMany(p => p.EnvironmentalReadings).FirstOrDefault();
        if (referenceReading == null) return;

        var lightValue = GetLightValueFromJson(referenceReading.ExtraData);
        var envDataResult = await environmentalDataProvider.GetEnvironmentalDataForAnalysisAsync(
            crop.CityName, lightValue, parameters.LightIntensityThreshold, referenceReading.Temperature, referenceReading.Humidity);

        if (envDataResult.IsFailure || !envDataResult.Value.IsConditionSuitable)
        {
            _logger.LogWarning("Las condiciones ambientales para el cultivo {CropName} no son adecuadas para el análisis.", crop.Name);
            return;
        }
        var envData = envDataResult.Value;

        // 4. Calcular Líneas Base T_wet y T_dry
        var controlPlantsData = plantsData.Where(p => p.Plant.ExperimentalGroup == ExperimentalGroupType.CONTROL);
        var stressPlantsData = plantsData.Where(p => p.Plant.ExperimentalGroup == ExperimentalGroupType.STRESS);

        var wetTemperatures = controlPlantsData.SelectMany(p => p.ThermalCaptures).Select(tc => DeserializeThermalStats(tc.ThermalDataStats)?.Avg_Temp ?? 0).Where(t => t > 0).ToList();
        var dryTemperatures = stressPlantsData.SelectMany(p => p.ThermalCaptures).Select(tc => DeserializeThermalStats(tc.ThermalDataStats)?.Avg_Temp ?? 0).Where(t => t > 0).ToList();

        if (!wetTemperatures.Any() || !dryTemperatures.Any())
        {
            _logger.LogWarning("No hay suficientes datos de plantas 'Control' o 'Stress' para calcular las líneas base en el cultivo {CropName}.", crop.Name);
            return;
        }

        double tWet = wetTemperatures.Average();
        double tDry = dryTemperatures.Max();

        // 5. Analizar cada planta 'Monitored'
        var monitoredPlantsData = plantsData.Where(p => p.Plant.ExperimentalGroup == ExperimentalGroupType.MONITORED);
        foreach (var plantData in monitoredPlantsData)
        {
            var plantTc = CalculateCanopyTemperature(plantData);
            if (!plantTc.HasValue) continue;

            double cwsi = (tDry - tWet > 0) ? (plantTc.Value - tWet) / (tDry - tWet) : 0;
            cwsi = Math.Clamp(cwsi, 0, 1); // Asegurar que el valor esté entre 0 y 1

            var previousStatus = plantData.Plant.Status;
            var newStatus = DetermineStatus(cwsi, parameters, previousStatus);

            // 6. Guardar resultado y disparar alerta si es necesario
            var analysisResult = new AnalysisResult
            {
                PlantId = plantData.Plant.Id,
                RecordedAt = nowUtc,
                CwsiValue = (float)cwsi,
                Status = newStatus,
                CanopyTemperature = (float)plantTc.Value,
                AmbientTemperature = (float)envData.AmbientTemperatureC,
                Vpd = (float)envData.VpdKpa,
                BaselineTwet = (float)tWet,
                BaselineTdry = (float)tDry
            };

            dbContext.AnalysisResults.Add(analysisResult);

            if (newStatus != previousStatus)
            {
                // --- INICIO DE LA CORRECCIÓN ---
                // Llamar al servicio de alertas ANTES de cambiar el estado en la base de datos
                await alertTriggerService.TriggerStressAlertAsync(
                    plantData.Plant.Id,
                    plantData.Plant.Name,
                    newStatus,
                    previousStatus,
                    (float)cwsi
                );

                // Actualizar el estado de la planta en la entidad
                var plantToUpdate = await dbContext.Plants.FindAsync(plantData.Plant.Id);
                if (plantToUpdate != null)
                {
                    plantToUpdate.Status = newStatus;
                    plantToUpdate.UpdatedAt = nowUtc;
                }
                // --- FIN DE LA CORRECCIÓN ---
            }
        }

        await dbContext.SaveChangesAsync(token);
        _logger.LogInformation("Ciclo de análisis completado para el cultivo: {CropName}", crop.Name);
    }

    private float? CalculateCanopyTemperature(PlantRawDataDto plantData)
    {
        var lastCapture = plantData.ThermalCaptures.OrderByDescending(tc => tc.RecordedAtServer).FirstOrDefault();
        if (lastCapture == null) return null;

        var stats = DeserializeThermalStats(lastCapture.ThermalDataStats);
        if (stats == null) return null;

        // Lógica de máscara/fallback
        if (!string.IsNullOrWhiteSpace(plantData.Plant.ThermalMaskData) && stats.Temperatures != null)
        {
            try
            {
                var mask = JsonSerializer.Deserialize<ThermalMask>(plantData.Plant.ThermalMaskData);
                if (mask?.Coordinates != null && mask.Coordinates.Any())
                {
                    var maskedTemperatures = new List<float>();
                    foreach (var coord in mask.Coordinates)
                    {
                        int index = coord.Y * 32 + coord.X; // Asumiendo 32x24
                        if (index < stats.Temperatures.Count)
                        {
                            maskedTemperatures.Add(stats.Temperatures[index].Value);
                        }
                    }
                    return maskedTemperatures.Any() ? maskedTemperatures.Average() : stats.Avg_Temp;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar máscara térmica para la planta {PlantId}", plantData.Plant.Id);
                return stats.Avg_Temp; // Fallback a avg_temp si la máscara falla
            }
        }

        return stats.Avg_Temp; // Fallback si no hay máscara
    }

    private PlantStatus DetermineStatus(double cwsi, AnalysisParameters parameters, PlantStatus previousStatus)
    {
        PlantStatus newStatus;
        if (cwsi > parameters.CwsiThresholdCritical)
        {
            newStatus = PlantStatus.SEVERE_STRESS;
        }
        else if (cwsi > parameters.CwsiThresholdIncipient)
        {
            newStatus = PlantStatus.MILD_STRESS;
        }
        else
        {
            newStatus = PlantStatus.OPTIMAL;
        }

        // Lógica de recuperación
        bool wasStressed = previousStatus == PlantStatus.MILD_STRESS || previousStatus == PlantStatus.SEVERE_STRESS;
        if (wasStressed && newStatus == PlantStatus.OPTIMAL)
        {
            return PlantStatus.RECOVERING;
        }

        // Si ya estaba en recuperación y sigue óptimo, se considera recuperado.
        if (previousStatus == PlantStatus.RECOVERING && newStatus == PlantStatus.OPTIMAL)
        {
            return PlantStatus.OPTIMAL;
        }

        return newStatus;
    }

    // Métodos auxiliares para deserializar JSON de forma segura
    private float? GetLightValueFromJson(string? extraDataJson)
    {
        if (string.IsNullOrWhiteSpace(extraDataJson)) return null;
        try
        {
            using var jsonDoc = JsonDocument.Parse(extraDataJson);
            if (jsonDoc.RootElement.TryGetProperty("light", out var lightElement) && lightElement.TryGetSingle(out var lightValue)) return lightValue;
        }
        catch { /* Ignorar error */ }
        return null;
    }

    private ThermalDataDto? DeserializeThermalStats(string? thermalDataJson)
    {
        if (string.IsNullOrEmpty(thermalDataJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<ThermalDataDto>(thermalDataJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { /* Ignorar error */ }
        return null;
    }

    // DTOs internos para el parseo de JSON
    private class ThermalMask
    {
        public List<Coord>? Coordinates { get; set; }
    }
    private class Coord
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}