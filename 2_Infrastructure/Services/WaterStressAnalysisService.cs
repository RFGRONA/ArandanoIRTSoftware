using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ArandanoIRT.Web._2_Infrastructure.Services;

/// <summary>
///     Un servicio en segundo plano que realiza el análisis de estrés hídrico a intervalos regulares.
///     Orquesta el proceso de recolección de datos, cálculo de CWSI y actualización del estado de las plantas.
/// </summary>
public class WaterStressAnalysisService : BackgroundService
{
    private readonly ILogger<WaterStressAnalysisService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundJobSettings _settings;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="WaterStressAnalysisService" />.
    /// </summary>
    public WaterStressAnalysisService(
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundJobSettings> settings,
        ILogger<WaterStressAnalysisService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    ///     Método principal del servicio. Se ejecuta en un bucle periódico según el intervalo configurado.
    ///     Para cada cultivo, verifica si la hora actual está dentro de la ventana de análisis definida en su configuración
    ///     antes de iniciar un ciclo de análisis.
    /// </summary>
    /// <param name="stoppingToken">Token que indica cuándo se debe detener el servicio.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_settings.AnalysisIntervalMinutes);
        using var timer = new PeriodicTimer(interval);
        _logger.LogInformation("Servicio de Análisis de Estrés Hídrico iniciado. Verificando cada {Minutes} minutos.",
            _settings.AnalysisIntervalMinutes);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var crops = await dbContext.Crops
                .AsNoTracking()
                .ToListAsync(stoppingToken);

            foreach (var crop in crops)
            {
                var parameters = crop.CropSettings.AnalysisParameters;
                var nowUtc = DateTime.UtcNow;

                if (!nowUtc.IsWithinColombiaTimeWindow(parameters.AnalysisWindowStartHour,
                        parameters.AnalysisWindowEndHour))
                    continue;

                _logger.LogInformation("Ventana de análisis activa para {CropName}. Iniciando ciclo.", crop.Name);
                await RunAnalysisCycleAsync(scope.ServiceProvider, crop, nowUtc, stoppingToken);
            }
        }
    }

    /// <summary>
    ///     Orquesta un ciclo completo de análisis para un cultivo específico.
    ///     Este método obtiene las plantas, recolecta sus datos crudos más recientes, delega el cálculo del CWSI
    ///     al IAnalysisExecutionService, guarda los resultados y finalmente actualiza los estados y dispara las alertas.
    /// </summary>
    private async Task RunAnalysisCycleAsync(IServiceProvider services, Crop crop, DateTime nowUtc,
        CancellationToken token)
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var analysisExecutionService = services.GetRequiredService<IAnalysisExecutionService>();
        var alertTriggerService = services.GetRequiredService<IAlertTriggerService>();

        var controlPlant = await dbContext.Plants.AsNoTracking()
            .FirstOrDefaultAsync(p => p.CropId == crop.Id && p.ExperimentalGroup == ExperimentalGroupType.CONTROL,
                token);

        if (controlPlant == null)
        {
            _logger.LogWarning("El cultivo {CropName} no tiene una planta de 'Control'. Se omite el ciclo de análisis.",
                crop.Name);
            return;
        }

        var monitoredPlants = await dbContext.Plants.AsNoTracking()
            .Where(p => p.CropId == crop.Id && p.ExperimentalGroup == ExperimentalGroupType.MONITORED)
            .ToListAsync(token);

        if (!monitoredPlants.Any()) return;

        var startTime = nowUtc.AddMinutes(-_settings.AnalysisIntervalMinutes);
        var newAnalysisResults = new List<AnalysisResult>();

        var controlCapture = await dbContext.ThermalCaptures.AsNoTracking()
            .Where(tc => tc.PlantId == controlPlant.Id && tc.RecordedAtServer >= startTime)
            .OrderByDescending(tc => tc.RecordedAtServer).FirstOrDefaultAsync(token);

        if (controlCapture == null) return;

        foreach (var plant in monitoredPlants)
        {
            var reading = await dbContext.EnvironmentalReadings.AsNoTracking()
                .Where(er => er.PlantId == plant.Id && er.RecordedAtServer >= startTime)
                .OrderByDescending(er => er.RecordedAtServer).FirstOrDefaultAsync(token);

            var capture = await dbContext.ThermalCaptures.AsNoTracking()
                .Where(tc => tc.PlantId == plant.Id && tc.RecordedAtServer >= startTime)
                .OrderByDescending(tc => tc.RecordedAtServer).FirstOrDefaultAsync(token);

            if (reading == null || capture == null) continue;

            var input = new IAnalysisExecutionService.CwsiCalculationInput(reading, capture, controlCapture, plant,
                controlPlant);
            var calculationResult = await analysisExecutionService.CalculateCwsiAsync(input);

            if (calculationResult.IsSuccess) newAnalysisResults.Add(calculationResult.Value);
        }

        if (!newAnalysisResults.Any()) return;

        await dbContext.AnalysisResults.AddRangeAsync(newAnalysisResults, token);
        await dbContext.SaveChangesAsync(token);

        await UpdateStatusesAndTriggerAlertsAsync(dbContext, alertTriggerService, newAnalysisResults,
            crop.CropSettings.AnalysisParameters, nowUtc);

        _logger.LogInformation("Ciclo de análisis completado para {CropName}. Se generaron {Count} nuevos registros.",
            crop.Name, newAnalysisResults.Count);
    }

    /// <summary>
    ///     Procesa los resultados de un ciclo de análisis para actualizar el estado de las plantas y disparar alertas si es
    ///     necesario.
    /// </summary>
    private async Task UpdateStatusesAndTriggerAlertsAsync(ApplicationDbContext dbContext,
        IAlertTriggerService alertTriggerService,
        List<AnalysisResult> results, AnalysisParameters parameters, DateTime nowUtc)
    {
        var plantIds = results.Select(r => r.PlantId).ToList();
        var plantsToUpdate = await dbContext.Plants.Where(p => plantIds.Contains(p.Id)).ToListAsync();

        foreach (var plant in plantsToUpdate)
        {
            var result = results.First(r => r.PlantId == plant.Id);
            var previousStatus = plant.Status;
            var newStatus = DetermineStatus(result.CwsiValue ?? 0, parameters, previousStatus);

            if (newStatus != previousStatus)
            {
                await alertTriggerService.TriggerStressAlertAsync(plant.Id, plant.Name, newStatus, previousStatus,
                    result.CwsiValue ?? 0);

                dbContext.PlantStatusHistories.Add(new PlantStatusHistory
                {
                    PlantId = plant.Id,
                    Status = newStatus,
                    ChangedAt = nowUtc,
                    Observation = $"Cambio de estado automático por sistema. CWSI: {result.CwsiValue:F2}."
                });

                plant.Status = newStatus;
                plant.UpdatedAt = nowUtc;
            }
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    ///     Determina el nuevo estado de una planta basado en su valor de CWSI y su estado anterior.
    ///     Incluye lógica para manejar el estado de "Recuperación".
    /// </summary>
    private PlantStatus DetermineStatus(double cwsi, AnalysisParameters parameters, PlantStatus previousStatus)
    {
        PlantStatus newStatus;
        if (cwsi > parameters.CwsiThresholdCritical) newStatus = PlantStatus.SEVERE_STRESS;
        else if (cwsi > parameters.CwsiThresholdIncipient) newStatus = PlantStatus.MILD_STRESS;
        else newStatus = PlantStatus.OPTIMAL;

        var wasStressed = previousStatus is PlantStatus.MILD_STRESS or PlantStatus.SEVERE_STRESS;
        if (wasStressed && newStatus == PlantStatus.OPTIMAL) return PlantStatus.RECOVERING;
        if (previousStatus == PlantStatus.RECOVERING && newStatus == PlantStatus.OPTIMAL) return PlantStatus.OPTIMAL;

        return newStatus;
    }
}