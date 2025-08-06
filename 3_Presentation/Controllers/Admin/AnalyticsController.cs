using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize] 
public class AnalyticsController : BaseAdminController
{
    private readonly IPlantService _plantService;
    private readonly IDataQueryService _dataQueryService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IPdfGeneratorService _pdfGeneratorService;
    private readonly IAlertService _alertService;

    public AnalyticsController(IPlantService plantService, IDataQueryService dataQueryService, IAnalyticsService analyticsService, IPdfGeneratorService pdfGeneratorService, IAlertService alertService)
    {
        _plantService = plantService;
        _dataQueryService = dataQueryService;
        _analyticsService = analyticsService;
        _pdfGeneratorService = pdfGeneratorService;
        _alertService = alertService;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _analyticsService.GetCropsForMonitoringAsync();

        if (result.IsFailure)
        {
            return View(new List<CropMonitorViewModel>());
        }

        return View(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, DateTime? startDate, DateTime? endDate)
    {
        var result = await _analyticsService.GetAnalysisDetailsAsync(id, startDate, endDate);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GenerateReport(int plantId, DateTime startDate, DateTime endDate)
    {
        // 1. Llamar al servicio que hemos creado para generar el array de bytes del PDF
        var pdfBytes = await _pdfGeneratorService.GeneratePlantReportAsync(plantId, startDate, endDate);

        // 2. Comprobar si el servicio devolvió un archivo válido
        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            TempData["ErrorMessage"] = "No se pudo generar el reporte. Es posible que no haya datos en el periodo seleccionado.";
            return RedirectToAction("Details", new { id = plantId, startDate, endDate });
        }

        // 3. Devolver el archivo al navegador para su descarga
        var fileName = $"Reporte_Estado_Hidrico_Planta_{plantId}_{DateTime.UtcNow.ToColombiaTime():yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> CreateMask(int id)
    {
        var plantResult = await _plantService.GetPlantByIdAsync(id);
        if (plantResult.IsFailure || plantResult.Value == null)
        {
            TempData[ErrorMessageKey] = "Planta no encontrada.";
            return RedirectToAction("Index", "Plants");
        }

        var captureResult = await _dataQueryService.GetLatestCaptureForMaskAsync(id);
        if (captureResult.IsFailure || captureResult.Value.Stats == null)
        {
            TempData["ErrorMessage"] = "No se encontró una captura térmica con imagen RGB y matriz de datos para esta planta.";
            return RedirectToAction("Details", "Plants", new { id });
        }

        var (stats, imagePath) = captureResult.Value;

        var viewModel = new MaskCreatorViewModel
        {
            PlantId = plantResult.Value.Id,
            PlantName = plantResult.Value.Name,
            RgbImagePath = imagePath,
            Temperatures = stats.Temperatures,
            MinTemp = stats.Min_Temp,
            MaxTemp = stats.Max_Temp,
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendReportByEmail(int plantId, DateTime startDate, DateTime endDate, string recipientEmail)
    {
        var plant = await _plantService.GetPlantByIdAsync(plantId); // Necesitamos el nombre de la planta
        if (plant.IsFailure || plant.Value == null)
        {
            TempData["ErrorMessage"] = "No se pudo encontrar la planta para enviar el reporte.";
            return RedirectToAction("Index");
        }

        var pdfBytes = await _pdfGeneratorService.GeneratePlantReportAsync(plantId, startDate, endDate);
        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            TempData["ErrorMessage"] = "No se pudo generar el reporte para enviar.";
            return RedirectToAction("Details", new { id = plantId, startDate, endDate });
        }

        await _alertService.SendReportByEmailAsync(recipientEmail, plant.Value.Name, pdfBytes);

        TempData["SuccessMessage"] = $"Reporte enviado exitosamente a {recipientEmail}.";
        return RedirectToAction("Details", new { id = plantId, startDate, endDate });
    }
}