using System.Text.Json;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

/// <summary>
///     Implementación del servicio de analíticas.
///     Se encarga de preparar y procesar los datos para las vistas de monitoreo y análisis.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="AnalyticsService" />.
    /// </summary>
    public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

            cropViewModel.AnalysisReadiness.HasControlGroup = controlPlants.Any();
            cropViewModel.AnalysisReadiness.HasMonitoredGroup =
                crop.Plants.Any(p => p.ExperimentalGroup == ExperimentalGroupType.MONITORED);
            cropViewModel.AnalysisReadiness.HasControlWithMask =
                controlPlants.Any(p => !string.IsNullOrEmpty(p.ThermalMaskData));

            // Se marca la parte de STRESS como "lista" para simplificar la lógica en la UI,
            // ya que el nuevo modelo de cálculo no depende de una planta de estrés.
            cropViewModel.AnalysisReadiness.HasStressGroup = true;
            cropViewModel.AnalysisReadiness.HasStressWithMask = true;

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

    /// <inheritdoc />
    public async Task<Result<AnalysisDetailsViewModel>> GetAnalysisDetailsAsync(int plantId, DateTime? startDate,
        DateTime? endDate)
    {
        var plant = await _context.Plants.Include(p => p.Crop).FirstOrDefaultAsync(p => p.Id == plantId);
        if (plant == null) return Result.Failure<AnalysisDetailsViewModel>("Planta no encontrada.");

        // Se simplifica la validación: solo se requiere una planta de control en el cultivo para proceder.
        var hasControlPlant = await _context.Plants.AnyAsync(p =>
            p.CropId == plant.CropId && p.ExperimentalGroup == ExperimentalGroupType.CONTROL);
        if (!hasControlPlant)
            return Result.Failure<AnalysisDetailsViewModel>(
                "La configuración del cultivo es inválida. Se requiere al menos una planta de tipo 'Control' para realizar análisis.");

        var displayEndDate = endDate ?? DateTime.Now.Date;
        var displayStartDate = startDate ?? displayEndDate.AddDays(-7);

        var queryStartDate = displayStartDate.ToSafeUniversalTime();
        var queryEndDate = displayEndDate.Date.AddDays(1).AddTicks(-1).ToSafeUniversalTime();

        var analysisData = await _context.AnalysisResults
            .AsNoTracking()
            .Where(ar => ar.PlantId == plantId && ar.RecordedAt >= queryStartDate && ar.RecordedAt < queryEndDate)
            .OrderBy(ar => ar.RecordedAt)
            .ToListAsync();

        // Si no se encuentran datos de análisis, se devuelve un modelo de vista vacío pero válido para la UI.
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