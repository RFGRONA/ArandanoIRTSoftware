using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
public class AnalyticsController : BaseAdminController
{
    private readonly IPlantService _plantService;
    private readonly IDataQueryService _dataQueryService;
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IPlantService plantService, IDataQueryService dataQueryService, IAnalyticsService analyticsService)
    {
        _plantService = plantService;
        _dataQueryService = dataQueryService;
        _analyticsService = analyticsService;
    }

    [HttpGet]
    public async Task<IActionResult> CreateMask(int id) // id de la planta
    {
        var plantResult = await _plantService.GetPlantByIdAsync(id);
        if (plantResult.IsFailure || plantResult.Value == null)
        {
            TempData[ErrorMessageKey] = "Planta no encontrada.";
            return RedirectToAction("Index", "Plants");
        }

        var captureResult = await _dataQueryService.GetLatestCaptureForMaskAsync(id);
        if (captureResult.IsFailure || captureResult.Value == null)
        {
            TempData[ErrorMessageKey] = "No se encontró una captura térmica con imagen RGB para esta planta.";
            return RedirectToAction("Details", "Plants", new { id });
        }

        var viewModel = new MaskCreatorViewModel()
        {
            PlantId = plantResult.Value.Id,
            PlantName = plantResult.Value.Name,
            RgbImagePath = captureResult.Value.RgbImagePath,
            Temperatures = captureResult.Value.Temperatures,
            MinTemp = captureResult.Value.Min_Temp,
            MaxTemp = captureResult.Value.Max_Temp,
            ExistingMaskJson = plantResult.Value.ThermalMaskData ?? "[]"
        };
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMask(int plantId, string coordinates)
    {
        var result = await _analyticsService.SaveThermalMaskAsync(plantId, coordinates);
        return HandleServiceResult(result, "Details", new { controller = "Plants", id = plantId });
    }
}