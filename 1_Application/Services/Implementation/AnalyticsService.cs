using System.Text.Json;
using ArandanoIRT.Web._0_Domain.Common;
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

    public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> SaveThermalMaskAsync(int plantId, string maskCoordinatesJson)
    {
        var plant = await _context.Plants.FindAsync(plantId);
        if (plant == null) return Result.Failure("Planta no encontrada.");
        try
        {
            var maskObject = new
            {
                thermal_mask = new
                { type = "points", coordinates = JsonSerializer.Deserialize<object>(maskCoordinatesJson) }
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
        var crops = await _context.Crops
            .AsNoTracking()
            .Include(c => c.Plants)
            .ToListAsync();

        var resultList = new List<CropMonitorViewModel>();
        foreach (var crop in crops)
        {
            var cropViewModel = new CropMonitorViewModel { Id = crop.Id, Name = crop.Name };

            var controlPlants = crop.Plants.Where(p => p.ExperimentalGroup == ExperimentalGroupType.CONTROL).ToList();

            // --- CAMBIO: La lógica de "listo para análisis" ya no depende de la planta de STRESS ---
            cropViewModel.AnalysisReadiness.HasControlGroup = controlPlants.Any();
            cropViewModel.AnalysisReadiness.HasMonitoredGroup =
                crop.Plants.Any(p => p.ExperimentalGroup == ExperimentalGroupType.MONITORED);
            cropViewModel.AnalysisReadiness.HasControlWithMask =
                controlPlants.Any(p => !string.IsNullOrEmpty(p.ThermalMaskData));

            // Marcamos la parte de STRESS como "lista" para no confundir al usuario en la UI
            cropViewModel.AnalysisReadiness.HasStressGroup = true;
            cropViewModel.AnalysisReadiness.HasStressWithMask = true;
            // --- FIN DEL CAMBIO ---

            foreach (var plant in crop.Plants)
                cropViewModel.Plants.Add(new PlantMonitorViewModel
                {
                    Id = plant.Id,
                    Name = plant.Name,
                    Status = plant.Status,
                    HasMask = !string.IsNullOrEmpty(plant.ThermalMaskData),
                    ExperimentalGroup = plant.ExperimentalGroup
                });
            resultList.Add(cropViewModel);
        }

        return Result.Success(resultList);
    }

    public async Task<Result<AnalysisDetailsViewModel>> GetAnalysisDetailsAsync(int plantId, DateTime? startDate,
        DateTime? endDate)
    {
        var plant = await _context.Plants.Include(p => p.Crop).FirstOrDefaultAsync(p => p.Id == plantId);
        if (plant == null) return Result.Failure<AnalysisDetailsViewModel>("Planta no encontrada.");

        // --- CAMBIO: Simplificamos la validación. Solo necesitamos una planta de control ---
        var hasControlPlant = await _context.Plants.AnyAsync(p =>
            p.CropId == plant.CropId && p.ExperimentalGroup == ExperimentalGroupType.CONTROL);
        if (!hasControlPlant)
            return Result.Failure<AnalysisDetailsViewModel>(
                "La configuración del cultivo es inválida. Se requiere al menos una planta de tipo 'Control' para realizar análisis.");
        // --- FIN DEL CAMBIO ---

        var displayEndDate = endDate ?? DateTime.Now.Date;
        var displayStartDate = startDate ?? displayEndDate.AddDays(-7);

        var queryStartDate = displayStartDate.ToSafeUniversalTime();
        var queryEndDate = displayEndDate.Date.AddDays(1).AddTicks(-1).ToSafeUniversalTime();

        var analysisData = await _context.AnalysisResults
            .AsNoTracking()
            .Where(ar => ar.PlantId == plantId && ar.RecordedAt >= queryStartDate && ar.RecordedAt < queryEndDate)
            .OrderBy(ar => ar.RecordedAt)
            .ToListAsync();

        // --- CAMBIO: Eliminamos la lógica de fallback y manejamos el caso "sin datos" de forma limpia ---
        if (!analysisData.Any())
        {
            _logger.LogWarning("No se encontraron datos de análisis para la planta {PlantId} en el rango solicitado.",
                plantId);
            var emptyViewModel = new AnalysisDetailsViewModel
            {
                PlantId = plant.Id,
                PlantName = plant.Name,
                CropName = plant.Crop.Name,
                StartDate = displayStartDate,
                EndDate = displayEndDate,
                HasData = false,
                CwsiThresholdIncipient =
                    (float)(plant.Crop.CropSettings?.AnalysisParameters.CwsiThresholdIncipient ?? 0.3),
                CwsiThresholdCritical =
                    (float)(plant.Crop.CropSettings?.AnalysisParameters.CwsiThresholdCritical ?? 0.5)
            };
            return Result.Success(emptyViewModel);
        }
        // --- FIN DEL CAMBIO ---

        var labels = analysisData.Select(ar => ar.RecordedAt.ToColombiaTime().ToString("dd/MM HH:mm")).ToList();
        var cwsiChartData = new
        {
            labels,
            datasets = new[]
            {
                new
                {
                    label = "CWSI", data = analysisData.Select(ar => ar.CwsiValue), borderColor = "rgb(75, 192, 192)",
                    tension = 0.1
                }
            }
        };
        var tempChartData = new
        {
            labels,
            datasets = new[]
            {
                new
                {
                    label = "T. Canopia (°C)", data = analysisData.Select(ar => ar.CanopyTemperature),
                    borderColor = "rgb(255, 99, 132)", tension = 0.1
                },
                new
                {
                    label = "T. Ambiente (°C)", data = analysisData.Select(ar => ar.AmbientTemperature),
                    borderColor = "rgb(54, 162, 235)", tension = 0.1
                }
            }
        };

        var viewModel = new AnalysisDetailsViewModel
        {
            PlantId = plant.Id,
            PlantName = plant.Name,
            CropName = plant.Crop.Name,
            StartDate = displayStartDate,
            EndDate = displayEndDate,
            HasData = true,
            CwsiChartDataJson = JsonSerializer.Serialize(cwsiChartData),
            TempChartDataJson = JsonSerializer.Serialize(tempChartData),
            CwsiThresholdIncipient = (float)(plant.Crop.CropSettings?.AnalysisParameters.CwsiThresholdIncipient ?? 0.3),
            CwsiThresholdCritical = (float)(plant.Crop.CropSettings?.AnalysisParameters.CwsiThresholdCritical ?? 0.5)
        };

        return Result.Success(viewModel);
    }
}