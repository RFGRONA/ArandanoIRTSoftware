using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize]
public class AnalyticsController : BaseAdminController
{
    private readonly IAlertService _alertService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IDataQueryService _dataQueryService;
    private readonly IPdfGeneratorService _pdfGeneratorService;
    private readonly IPlantService _plantService;

    public AnalyticsController(IPlantService plantService, IDataQueryService dataQueryService,
        IAnalyticsService analyticsService, IPdfGeneratorService pdfGeneratorService, IAlertService alertService)
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

        if (result.IsFailure) return View(new List<CropMonitorViewModel>());

        return View(result.Value);
    }

    [HttpGet]
    [HttpGet]
    public async Task<IActionResult> Details(int id, DateTime? startDate, DateTime? endDate)
    {
        var originalStartDate = startDate?.Date;
        var originalEndDate = endDate?.Date;

        var requestedEndDate = (endDate ?? DateTime.Now).Date;
        var requestedStartDate = (startDate ?? requestedEndDate.AddDays(-7)).Date;

        if (requestedStartDate > requestedEndDate)
            (requestedStartDate, requestedEndDate) = (requestedEndDate, requestedStartDate);

        var result = await _analyticsService.GetAnalysisDetailsAsync(id, requestedStartDate, requestedEndDate);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        if ((originalStartDate.HasValue && result.Value.StartDate.Date != originalStartDate.Value)
            || (originalEndDate.HasValue && result.Value.EndDate.Date != originalEndDate.Value))
            return RedirectToAction(nameof(Details), new
            {
                id,
                startDate = result.Value.StartDate.ToString("yyyy-MM-dd"),
                endDate = result.Value.EndDate.ToString("yyyy-MM-dd")
            });

        return View(result.Value);
    }


    [HttpGet]
    public async Task<IActionResult> GenerateReport(int plantId, DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate) (startDate, endDate) = (endDate, startDate);

        var utcStartDate = startDate.ToSafeUniversalTime();
        var utcEndDate = endDate.Date.AddDays(1).AddTicks(-1).ToSafeUniversalTime();

        // 1. Llamar al servicio que hemos creado para generar el array de bytes del PDF
        var pdfBytes = await _pdfGeneratorService.GeneratePlantReportAsync(plantId, utcStartDate, utcEndDate);

        // 2. Comprobar si el servicio devolvió un archivo válido
        if (pdfBytes.Length == 0)
        {
            TempData["ErrorMessage"] =
                "No se pudo generar el reporte. Es posible que no haya datos en el periodo seleccionado.";
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
            TempData["ErrorMessage"] =
                "No se encontró una captura térmica con imagen RGB y matriz de datos para esta planta.";
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
    public async Task<IActionResult> SaveMask(int id, string coordinates)
    {
        var result = await _analyticsService.SaveThermalMaskAsync(id, coordinates);

        if (result.IsSuccess)
        {
            TempData[SuccessMessageKey] = "Máscara guardada exitosamente.";
            return RedirectToAction("Details", "Plants", new { id });
        }

        TempData[ErrorMessageKey] = result.ErrorMessage;
        return RedirectToAction("CreateMask", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendReportByEmail(int plantId, DateTime startDate, DateTime endDate,
        string recipientEmail)
    {
        var plant = await _plantService.GetPlantByIdAsync(plantId);
        if (plant.IsFailure || plant.Value == null)
        {
            TempData["ErrorMessage"] = "No se pudo encontrar la planta para enviar el reporte.";
            return RedirectToAction("Index");
        }

        if (startDate > endDate) (startDate, endDate) = (endDate, startDate);

        var utcStartDate = startDate.ToSafeUniversalTime();
        var utcEndDate = endDate.Date.AddDays(1).AddTicks(-1).ToSafeUniversalTime();

        var pdfBytes = await _pdfGeneratorService.GeneratePlantReportAsync(plantId, utcStartDate, utcEndDate);
        if (pdfBytes.Length == 0)
        {
            TempData["ErrorMessage"] = "No se pudo generar el reporte para enviar.";
            return RedirectToAction("Details", new { id = plantId, startDate, endDate });
        }

        await _alertService.SendReportByEmailAsync(recipientEmail, plant.Value.Name, pdfBytes);

        TempData["SuccessMessage"] = $"Reporte enviado exitosamente a {recipientEmail}.";
        return RedirectToAction("Details", new { id = plantId, startDate, endDate });
    }
}