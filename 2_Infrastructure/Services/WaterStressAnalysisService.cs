using System.Text.Json;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
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
            var cropService = scope.ServiceProvider.GetRequiredService<ICropService>();

            var crops = await dbContext.Crops.AsNoTracking().ToListAsync(stoppingToken);

            foreach (var crop in crops)
            {
                var parametersResult = await cropService.GetAnalysisParametersAsync(crop.Id);
                if (parametersResult.IsFailure) continue;

                var parameters = parametersResult.Value.AnalysisParameters;
                var nowUtc = DateTime.UtcNow;

                if (!nowUtc.IsWithinColombiaTimeWindow(parameters.AnalysisWindowStartHour,
                        parameters.AnalysisWindowEndHour))
                    continue; // No estamos en la ventana de análisis para este cultivo

                _logger.LogInformation("Iniciando ciclo de análisis para el cultivo: {CropName}", crop.Name);
                await RunAnalysisCycleAsync(scope.ServiceProvider, crop, parameters, nowUtc, stoppingToken);
            }
        }
    }

    private async Task RunAnalysisCycleAsync(IServiceProvider services, Crop crop, AnalysisParameters parameters,
        DateTime nowUtc, CancellationToken token)
    {
        var analysisExecutionService = services.GetRequiredService<IAnalysisExecutionService>();
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        // 1. Obtener plantas monitoreadas del cultivo
        var monitoredPlants = await dbContext.Plants
            .Where(p => p.CropId == crop.Id && p.ExperimentalGroup == ExperimentalGroupType.MONITORED)
            .Select(p => p.Id)
            .ToListAsync(token);

        if (!monitoredPlants.Any())
        {
            _logger.LogWarning("El cultivo {CropName} no tiene plantas 'Monitored' para analizar.", crop.Name);
            return;
        }

        _logger.LogInformation("Delegando análisis a AnalysisExecutionService para {Count} plantas monitoreadas.", monitoredPlants.Count);

        foreach (var plantId in monitoredPlants)
        {
            await analysisExecutionService.ExecuteCatchUpForPlantAsync(plantId);
        }

        _logger.LogInformation("Ciclo de análisis completado para el cultivo: {CropName}", crop.Name);
    }
}