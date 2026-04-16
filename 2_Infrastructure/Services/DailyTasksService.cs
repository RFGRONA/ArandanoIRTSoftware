using System.Text.Json;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using JsonException = Newtonsoft.Json.JsonException;

namespace ArandanoIRT.Web._2_Infrastructure.Services;

public class DailyTasksService : BackgroundService
{
    private readonly AnomalyParametersSettings _anomalySettings;
    private readonly ILogger<DailyTasksService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DailyTasksService(
        IServiceScopeFactory scopeFactory,
        IOptions<AnomalyParametersSettings> anomalySettings,
        ILogger<DailyTasksService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _anomalySettings = anomalySettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // El timer se ejecutará cada hora, pero la lógica interna solo correrá a las 6 AM.
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        _logger.LogInformation("Servicio de Tareas Diarias iniciado.");

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            if (DateTime.UtcNow.ToColombiaTime().Hour != 6) continue; // Solo ejecutar a las 6:00 AM hora de Colombia

            _logger.LogInformation("Ejecutando ciclo de tareas diarias...");

            await using var scope = _scopeFactory.CreateAsyncScope();

            await RunAnomalyDetectionAsync(scope.ServiceProvider, stoppingToken);
            await RunMaskCreationCheckAsync(scope.ServiceProvider, stoppingToken);

            _logger.LogInformation("Ciclo de tareas diarias completado.");
        }
    }

    private async Task RunAnomalyDetectionAsync(IServiceProvider services, CancellationToken token)
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var alertTriggerService = services.GetRequiredService<IAlertTriggerService>();

        var yesterday = DateTime.UtcNow.AddDays(-1);
        var startTime = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 20, 0, 0, DateTimeKind.Utc);
        var endTime = startTime.AddHours(10); // Desde las 8 PM hasta las 6 AM del día siguiente

        // Incluimos tanto lecturas ambientales como capturas térmicas
        var plantsData = await dbContext.Plants
            .Where(p => p.ExperimentalGroup == ExperimentalGroupType.MONITORED)
            .Include(p => p.EnvironmentalReadings
                .Where(er => er.RecordedAtServer >= startTime && er.RecordedAtServer < endTime)
                .OrderBy(er => er.RecordedAtServer))
            .Include(p => p.ThermalCaptures
                .Where(tc => tc.RecordedAtServer >= startTime && tc.RecordedAtServer < endTime)
                .OrderBy(tc => tc.RecordedAtServer))
            .ToListAsync(token);

        foreach (var plant in plantsData)
        {
            var consecutiveAnomalies = 0;

            // No podemos analizar si no hay datos de ambos tipos
            if (!plant.EnvironmentalReadings.Any() || !plant.ThermalCaptures.Any()) continue;

            // Iteramos sobre las lecturas ambientales y buscamos la captura térmica más cercana
            foreach (var reading in plant.EnvironmentalReadings)
            {
                var closestCapture = plant.ThermalCaptures
                    .OrderBy(tc => Math.Abs((tc.RecordedAtServer - reading.RecordedAtServer).TotalSeconds))
                    .FirstOrDefault();

                // Si no hay captura térmica cercana (ej. a menos de 5 min), no podemos comparar
                if (closestCapture == null ||
                    Math.Abs((closestCapture.RecordedAtServer - reading.RecordedAtServer).TotalMinutes) > 5)
                {
                    consecutiveAnomalies = 0; // Rompemos la racha si no hay datos
                    continue;
                }

                var thermalStats = DeserializeThermalStats(closestCapture.ThermalDataStats);
                if (thermalStats?.Avg_Temp == null)
                {
                    consecutiveAnomalies = 0;
                    continue;
                }

                var canopyTemp = thermalStats.Avg_Temp;
                var airTemp = reading.Temperature;

                if (canopyTemp - airTemp > _anomalySettings.DeltaTThreshold)
                    consecutiveAnomalies++;
                else
                    consecutiveAnomalies = 0;

                if (consecutiveAnomalies >= 4)
                {
                    // Solo actualizamos y alertamos si el estado actual no es ya 'UNKNOWN'
                    if (plant.Status != PlantStatus.UNKNOWN)
                    {
                        _logger.LogWarning("Anomalía detectada para la planta {PlantName}", plant.Name);
                        var historyRecord = new PlantStatusHistory
                        {
                            PlantId = plant.Id,
                            Status = PlantStatus.UNKNOWN,
                            Observation = "Cambio de estado automático por detección de anomalía nocturna.",
                            UserId = null,
                            ChangedAt = DateTime.UtcNow
                        };
                        dbContext.PlantStatusHistories.Add(historyRecord);

                        plant.Status = PlantStatus.UNKNOWN;
                        dbContext.Update(plant);

                        await dbContext.SaveChangesAsync(token);
                        await alertTriggerService.TriggerAnomalyAlertAsync(plant.Id, plant.Name);
                    }

                    break; // Pasamos a la siguiente planta
                }
            }
        }
    }

    // Necesitamos este método auxiliar dentro de DailyTasksService
    private ThermalDataDto? DeserializeThermalStats(string? thermalDataJson)
    {
        if (string.IsNullOrEmpty(thermalDataJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<ThermalDataDto>(thermalDataJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "No se pudo deserializar ThermalDataStats en DailyTasksService.");
            return null;
        }
    }

    private async Task RunMaskCreationCheckAsync(IServiceProvider services, CancellationToken token)
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var alertTriggerService = services.GetRequiredService<IAlertTriggerService>();

        var plantsNeedingMask = await dbContext.Plants
            .Where(p => p.ExperimentalGroup == ExperimentalGroupType.MONITORED && p.ThermalMaskData == null)
            .Select(p => p.Name)
            .ToListAsync(token);

        if (plantsNeedingMask.Any()) await alertTriggerService.TriggerMaskCreationAlertAsync(plantsNeedingMask);
    }
}