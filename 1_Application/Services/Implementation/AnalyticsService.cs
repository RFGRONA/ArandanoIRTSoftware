using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using System.Text.Json;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> SaveThermalMaskAsync(int plantId, string maskCoordinatesJson)
    {
        var plant = await _context.Plants.FindAsync(plantId);
        if (plant == null)
        {
            return Result.Failure("Planta no encontrada.");
        }

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
                    Name = crop.Name,
                };

                // Lógica de validación
                var hasControl = crop.Plants.Any(p => p.ExperimentalGroup == ExperimentalGroupType.CONTROL);
                var hasStress = crop.Plants.Any(p => p.ExperimentalGroup == ExperimentalGroupType.STRESS);
                var hasMonitored = crop.Plants.Any(p => p.ExperimentalGroup == ExperimentalGroupType.MONITORED);

                if (hasControl && hasStress && hasMonitored)
                {
                    cropViewModel.IsConfigurationValid = true;
                    cropViewModel.ValidationMessage = "Configuración correcta para el análisis.";
                }
                else
                {
                    cropViewModel.IsConfigurationValid = false;
                    cropViewModel.ValidationMessage = "Configuración Incompleta: Se requiere al menos una planta de tipo 'Control', 'Stress' y 'Monitored' para activar el análisis.";
                }

                foreach (var plant in crop.Plants)
                {
                    cropViewModel.Plants.Add(new PlantMonitorViewModel
                    {
                        Id = plant.Id,
                        Name = plant.Name,
                        Status = plant.Status,
                        HasMask = !string.IsNullOrEmpty(plant.ThermalMaskData)
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

    public async Task<Result<AnalysisDetailsViewModel>> GetAnalysisDetailsAsync(int plantId, DateTime? startDate, DateTime? endDate)
    {
        // 1. Validar la configuración del cultivo
        var plant = await _context.Plants.Include(p => p.Crop).FirstOrDefaultAsync(p => p.Id == plantId);
        if (plant == null) return Result.Failure<AnalysisDetailsViewModel>("Planta no encontrada.");

        if (string.IsNullOrEmpty(plant.ThermalMaskData))
        {
            return Result.Failure<AnalysisDetailsViewModel>("La planta no tiene una máscara térmica definida y no puede ser analizada.");
        }

        var cropPlants = await _context.Plants.Where(p => p.CropId == plant.CropId).ToListAsync();
        if (!cropPlants.Any(p => p.ExperimentalGroup == ExperimentalGroupType.CONTROL) ||
            !cropPlants.Any(p => p.ExperimentalGroup == ExperimentalGroupType.STRESS))
        {
            return Result.Failure<AnalysisDetailsViewModel>("La configuración del cultivo es inválida para el análisis.");
        }

        // 2. Definir rango de fechas (default: últimos 7 días)
        var finalEndDate = endDate.HasValue
            ? endDate.Value.ToColombiaTime().AddDays(1).AddTicks(-1).ToUniversalTime()
            : DateTime.UtcNow;

        var finalStartDate = startDate.HasValue
            ? startDate.Value.ToColombiaTime().ToUniversalTime()
            : finalEndDate.AddDays(-7);

        // 3. Obtener datos de análisis
        var analysisData = await _context.AnalysisResults
            .AsNoTracking()
            .Where(ar => ar.PlantId == plantId && ar.RecordedAt >= finalStartDate && ar.RecordedAt <= finalEndDate)
            .OrderBy(ar => ar.RecordedAt)
            .ToListAsync();

        // 4. Formatear datos para Chart.js
        var labels = analysisData.Select(ar => ar.RecordedAt.ToColombiaTime().ToString("dd/MM HH:mm")).ToList();

        var cwsiChartData = new
        {
            labels,
            datasets = new[] {
                new {
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
            datasets = new[] {
                new {
                    label = "T. Canopia (°C)",
                    data = analysisData.Select(ar => ar.CanopyTemperature),
                    borderColor = "rgb(255, 99, 132)",
                    tension = 0.1
                },
                new {
                    label = "T. Ambiente (°C)",
                    data = analysisData.Select(ar => ar.AmbientTemperature),
                    borderColor = "rgb(54, 162, 235)",
                    tension = 0.1
                }
            }
        };

        // 5. Poblar y devolver el ViewModel
        var viewModel = new AnalysisDetailsViewModel
        {
            PlantId = plant.Id,
            PlantName = plant.Name,
            CropName = plant.Crop.Name,
            StartDate = finalStartDate,
            EndDate = finalEndDate,
            CwsiChartDataJson = JsonSerializer.Serialize(cwsiChartData),
            TempChartDataJson = JsonSerializer.Serialize(tempChartData),
            CwsiThresholdIncipient = 0.3f, // Estos valores podrían venir de la configuración
            CwsiThresholdCritical = 0.5f
        };

        return Result.Success(viewModel);
    }
}