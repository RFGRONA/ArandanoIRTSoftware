using System.Text.Json;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;
    private readonly IAnalysisExecutionService _analysisExecutionService;
    private readonly IServiceScopeFactory _scopeFactory;

    public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger, IAnalysisExecutionService analysisExecutionService, IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _logger = logger;
        _analysisExecutionService = analysisExecutionService;
        _scopeFactory = scopeFactory;
    }

    public async Task<Result> SaveThermalMaskAsync(int plantId, string maskCoordinatesJson)
    {
        var plant = await _context.Plants.FindAsync(plantId);
        if (plant == null) return Result.Failure("Planta no encontrada.");

        try
        {
            // Creamos el objeto JSON final que se almacenará en la base de datos
            var maskObject = new
            {
                thermal_mask = new
                {
                    type = "points",
                    coordinates = JsonSerializer.Deserialize<object>(maskCoordinatesJson)
                }
            };

            plant.ThermalMaskData = JsonSerializer.Serialize(maskObject);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Máscara térmica guardada exitosamente para la planta {PlantId}", plantId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar la máscara térmica para la planta {PlantId}", plantId);
            return Result.Failure("Error al procesar y guardar la máscara.");
        }
    }

    public async Task<Result<List<CropMonitorViewModel>>> GetCropsForMonitoringAsync()
    {
        try
        {
            var crops = await _context.Crops
                .AsNoTracking()
                .Include(c => c.Plants)
                .ToListAsync();

            var resultList = new List<CropMonitorViewModel>();

            foreach (var crop in crops)
            {
                var cropViewModel = new CropMonitorViewModel
                {
                    Id = crop.Id,
                    Name = crop.Name
                };

                // 1. Separar las plantas por grupo
                var controlPlants = crop.Plants.Where(p => p.ExperimentalGroup == ExperimentalGroupType.CONTROL).ToList();
                var stressPlants = crop.Plants.Where(p => p.ExperimentalGroup == ExperimentalGroupType.STRESS).ToList();
                var monitoredPlants = crop.Plants.Where(p => p.ExperimentalGroup == ExperimentalGroupType.MONITORED).ToList();

                // 2. Verificar cada una de las condiciones
                cropViewModel.AnalysisReadiness.HasControlGroup = controlPlants.Any();
                cropViewModel.AnalysisReadiness.HasStressGroup = stressPlants.Any();
                cropViewModel.AnalysisReadiness.HasMonitoredGroup = monitoredPlants.Any();

                // 3. Verificar si los grupos requeridos tienen al menos una máscara
                cropViewModel.AnalysisReadiness.HasControlWithMask = controlPlants.Any(p => !string.IsNullOrEmpty(p.ThermalMaskData));
                cropViewModel.AnalysisReadiness.HasStressWithMask = stressPlants.Any(p => !string.IsNullOrEmpty(p.ThermalMaskData));

                foreach (var plant in crop.Plants)
                {
                    cropViewModel.Plants.Add(new PlantMonitorViewModel
                    {
                        Id = plant.Id,
                        Name = plant.Name,
                        Status = plant.Status,
                        HasMask = !string.IsNullOrEmpty(plant.ThermalMaskData),
                        ExperimentalGroup = plant.ExperimentalGroup
                    });
                }

                resultList.Add(cropViewModel);
            }

            return Result.Success(resultList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener los cultivos para el monitoreo.");
            return Result.Failure<List<CropMonitorViewModel>>("Error interno al preparar los datos de monitoreo.");
        }
    }

    public async Task<Result<AnalysisDetailsViewModel>> GetAnalysisDetailsAsync(int plantId, DateTime? startDate,
        DateTime? endDate)
    {
        // 1. Validar la configuración del cultivo (sin cambios)
        var plant = await _context.Plants.Include(p => p.Crop).FirstOrDefaultAsync(p => p.Id == plantId);
        if (plant == null) return Result.Failure<AnalysisDetailsViewModel>("Planta no encontrada.");

        if (string.IsNullOrEmpty(plant.ThermalMaskData))
            return Result.Failure<AnalysisDetailsViewModel>(
                "La planta no tiene una máscara térmica definida y no puede ser analizada.");



        // 2. Definir las fechas de visualización. Estas siempre serán locales y sin parte de tiempo.
        var displayEndDate = endDate ?? DateTime.Now.Date;
        var displayStartDate = startDate ?? displayEndDate.AddDays(-7);

        // 3. Preparar las fechas para la consulta a la base de datos (convertidas a UTC)
        var queryStartDate = displayStartDate.ToSafeUniversalTime();
        var queryEndDate = displayEndDate.Date.AddDays(1).AddTicks(-1).ToSafeUniversalTime();

        _logger.LogInformation(
            "Ejecutando consulta de análisis para PlantId {PlantId} en el rango de fechas (UTC): {StartDate} a {EndDate}",
            plantId, queryStartDate, queryEndDate);

        var analysisData = await _context.AnalysisResults
            .AsNoTracking()
            .Where(ar => ar.PlantId == plantId && ar.RecordedAt >= queryStartDate && ar.RecordedAt < queryEndDate)
            .OrderBy(ar => ar.RecordedAt)
            .ToListAsync();

        // 4. Lógica de Fallback: Si no hay datos, buscar el último rango disponible
        if (!analysisData.Any())
        {
            _logger.LogInformation(
                "No se encontraron datos en el rango inicial. Buscando el último registro disponible...");
            var lastRecordDate = await _context.AnalysisResults
                .Where(ar => ar.PlantId == plantId)
                .OrderByDescending(ar => ar.RecordedAt)
                .Select(ar => ar.RecordedAt)
                .FirstOrDefaultAsync();

            if (lastRecordDate != default)
            {
                _logger.LogInformation("Último registro encontrado en {LastDate}. Ajustando el rango de fechas.",
                    lastRecordDate);

                displayEndDate = lastRecordDate.ToColombiaTime().Date;
                displayStartDate = displayEndDate.AddDays(-7);

                queryStartDate = displayStartDate.ToSafeUniversalTime();
                queryEndDate = displayEndDate.Date.AddDays(1).AddTicks(-1).ToSafeUniversalTime();

                analysisData = await _context.AnalysisResults
                    .AsNoTracking()
                    .Where(ar =>
                        ar.PlantId == plantId && ar.RecordedAt >= queryStartDate && ar.RecordedAt < queryEndDate)
                    .OrderBy(ar => ar.RecordedAt)
                    .ToListAsync();
            }
        }

        if (!analysisData.Any())
        {
            _logger.LogWarning("No se encontraron datos de análisis para la planta {PlantId}. Lanzando Catch-Up en Background...", plantId);

            Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var scopedExecutionService = scope.ServiceProvider.GetRequiredService<IAnalysisExecutionService>();
                try
                {
                    await scopedExecutionService.ExecuteCatchUpForPlantAsync(plantId);
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<AnalyticsService>>();
                    logger.LogError(ex, "Error crítico durante Catch-Up en background para la planta {PlantId}", plantId);
                }
            });

            return Result.Failure<AnalysisDetailsViewModel>("Sin datos de análisis actualizados. Reconstruyendo análisis de la planta en segundo plano. Consulte nuevamente en unos minutos.");
        }

        _logger.LogInformation("Consulta completada. Se encontraron {Count} registros de análisis.",
            analysisData.Count);

        var smoothingWindow = plant.Crop.CropSettings.AnalysisParameters.SmoothingWindowMinutes;
        var smoothedData = new List<AnalysisResult>();
        foreach (var record in analysisData)
        {
            var windowStart = record.RecordedAt.AddMinutes(-smoothingWindow);
            var pointsInWindow = analysisData
                .Where(ar => ar.RecordedAt >= windowStart && ar.RecordedAt <= record.RecordedAt)
                .ToList();

            var smoothedRecord = new AnalysisResult
            {
                RecordedAt = record.RecordedAt,
                CwsiValue = (float?)pointsInWindow.Average(p => p.CwsiValue),
                CanopyTemperature = (float?)pointsInWindow.Average(p => p.CanopyTemperature),
                AmbientTemperature = (float?)pointsInWindow.Average(p => p.AmbientTemperature),
                Vpd = (float?)pointsInWindow.Average(p => p.Vpd),
                BaselineLL = (float?)pointsInWindow.Average(p => p.BaselineLL),
                BaselineUL = (float?)pointsInWindow.Average(p => p.BaselineUL)
            };
            smoothedData.Add(smoothedRecord);
        }

        analysisData = smoothedData;

        // 5. Formatear datos para Chart.js
        var labels = analysisData.Select(ar => ar.RecordedAt.ToColombiaTime().ToString("dd/MM HH:mm")).ToList();

        var cwsiChartData = new
        {
            labels,
            datasets = new[]
            {
                new
                {
                    label = "CWSI",
                    data = analysisData.Select(ar => ar.CwsiValue),
                    borderColor = "rgb(75, 192, 192)",
                    tension = 0.1
                }
            }
        };

        var tempChartData = new
        {
            labels,
            datasets = new object[]
            {
                new
                {
                    label = "T. Canopia (°C)",
                    data = analysisData.Select(ar => ar.CanopyTemperature),
                    borderColor = "rgb(255, 99, 132)",
                    tension = 0.1
                },
                new
                {
                    label = "T. Ambiente (°C)",
                    data = analysisData.Select(ar => ar.AmbientTemperature),
                    borderColor = "rgb(54, 162, 235)",
                    tension = 0.1
                },
                new
                {
                    label = "Línea Base Empírica (LL)",
                    data = analysisData.Select(ar => ar.BaselineLL),
                    borderColor = "rgb(153, 102, 255)",
                    tension = 0.1
                },
                new
                {
                    label = "VPD (kPa)",
                    data = analysisData.Select(ar => ar.Vpd),
                    borderColor = "rgb(255, 159, 64)",
                    tension = 0.1,
                    yAxisID = "y1"
                }
            }
        };

        // 6. Poblar y devolver el ViewModel usando las fechas de visualización correctas
        var viewModel = new AnalysisDetailsViewModel
        {
            PlantId = plant.Id,
            PlantName = plant.Name,
            CropName = plant.Crop.Name,
            StartDate = displayStartDate,
            EndDate = displayEndDate,
            CwsiChartDataJson = JsonSerializer.Serialize(cwsiChartData),
            TempChartDataJson = JsonSerializer.Serialize(tempChartData),
            CwsiThresholdIncipient = (float)plant.Crop.CropSettings.AnalysisParameters.CwsiThresholdIncipient,
            CwsiThresholdCritical = (float)plant.Crop.CropSettings.AnalysisParameters.CwsiThresholdCritical
        };

        return Result.Success(viewModel);
    }

    public async Task<Result> ReanalyzePlantAsync(int plantId)
    {
        var plant = await _context.Plants.FindAsync(plantId);
        if (plant == null) return Result.Failure("Planta no encontrada.");

        var deletedRows = await _context.AnalysisResults
            .Where(ar => ar.PlantId == plantId)
            .ExecuteDeleteAsync();

        _logger.LogInformation("Eliminados {Count} resultados de análisis para la planta {PlantId} previo a la reevaluación.", deletedRows, plantId);

        Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var scopedExecutionService = scope.ServiceProvider.GetRequiredService<IAnalysisExecutionService>();
            try
            {
                await scopedExecutionService.ExecuteCatchUpForPlantAsync(plantId);
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<AnalyticsService>>();
                logger.LogError(ex, "Error crítico durante ReAnalysis en background para la planta {PlantId}", plantId);
            }
        });

        return Result.Success();
    }
}