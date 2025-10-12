using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

/// <summary>
///     Controlador que gestiona todas las vistas y acciones relacionadas con el análisis de datos.
///     Incluye el dashboard de monitoreo, los detalles de análisis por planta, la creación de máscaras y la generación de
///     reportes.
/// </summary>
[Area("Admin")]
[Authorize]
public class AnalyticsController : BaseAdminController
{
    private readonly IAlertService _alertService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IDataQueryService _dataQueryService;
    private readonly IPdfGeneratorService _pdfGeneratorService;
    private readonly IPlantService _plantService;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="AnalyticsController" />.
    /// </summary>
    public AnalyticsController(IPlantService plantService, IDataQueryService dataQueryService,
        IAnalyticsService analyticsService, IPdfGeneratorService pdfGeneratorService, IAlertService alertService,
        IBackgroundJobClient backgroundJobClient)
    {
        _plantService = plantService;
        _dataQueryService = dataQueryService;
        _analyticsService = analyticsService;
        _pdfGeneratorService = pdfGeneratorService;
        _alertService = alertService;
        _backgroundJobClient = backgroundJobClient;
    }

    /// <summary>
    ///     Muestra el dashboard principal de monitoreo con el estado de todos los cultivos y sus plantas.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var result = await _analyticsService.GetCropsForMonitoringAsync();
        if (result.IsFailure)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage;
            return View(new List<CropMonitorViewModel>());
        }

        return View(result.Value);
    }

    /// <summary>
    ///     Muestra la página de detalles de análisis para una planta específica en un rango de fechas.
    /// </summary>
    /// <param name="id">El ID de la planta a analizar.</param>
    /// <param name="startDate">La fecha de inicio del período de análisis.</param>
    /// <param name="endDate">La fecha de fin del período de análisis.</param>
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

    /// <summary>
    ///     Genera y descarga un informe en PDF del estado hídrico de una planta para un rango de fechas.
    /// </summary>
    /// <param name="plantId">El ID de la planta.</param>
    /// <param name="startDate">La fecha de inicio del reporte.</param>
    /// <param name="endDate">La fecha de fin del reporte.</param>
    /// <returns>Un `FileResult` que inicia la descarga del PDF.</returns>
    [HttpGet]
    public async Task<IActionResult> GenerateReport(int plantId, DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate) (startDate, endDate) = (endDate, startDate);

        var pdfBytes = await _pdfGeneratorService.GeneratePlantReportAsync(plantId, startDate, endDate);

        if (pdfBytes.Length == 0)
        {
            TempData["ErrorMessage"] =
                "No se pudo generar el reporte. Es posible que no haya datos en el periodo seleccionado.";
            return RedirectToAction("Details", new { id = plantId, startDate, endDate });
        }

        var fileName = $"Reporte_Estado_Hidrico_Planta_{plantId}_{DateTime.UtcNow.ToColombiaTime():yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    ///     Muestra la página interactiva para crear o editar una máscara térmica para una planta.
    /// </summary>
    /// <param name="id">El ID de la planta.</param>
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

    /// <summary>
    ///     Guarda las coordenadas de la máscara térmica enviadas desde el editor.
    /// </summary>
    /// <param name="id">El ID de la planta.</param>
    /// <param name="coordinates">La cadena JSON con las coordenadas de la máscara.</param>
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

    /// <summary>
    ///     Genera un reporte en PDF y lo envía por correo electrónico al destinatario especificado.
    /// </summary>
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

        var pdfBytes = await _pdfGeneratorService.GeneratePlantReportAsync(plantId, startDate, endDate);
        if (pdfBytes.Length == 0)
        {
            TempData["ErrorMessage"] = "No se pudo generar el reporte para enviar.";
            return RedirectToAction("Details", new { id = plantId, startDate, endDate });
        }

        await _alertService.SendReportByEmailAsync(recipientEmail, plant.Value.Name, pdfBytes);

        TempData["SuccessMessage"] = $"Reporte enviado exitosamente a {recipientEmail}.";
        return RedirectToAction("Details", new { id = plantId, startDate, endDate });
    }

    /// <summary>
    ///     Inicia un trabajo en segundo plano (usando Hangfire) para re-analizar todo el historial de datos de una planta.
    /// </summary>
    /// <param name="plantId">El ID de la planta a re-analizar.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult StartCatchUpAnalysis(int plantId)
    {
        // Se encola el trabajo de análisis en Hangfire para que se ejecute de forma asíncrona.
        _backgroundJobClient.Enqueue<IAnalysisExecutionService>(service =>
            service.ExecuteCatchUpForPlantAsync(plantId));

        TempData["SuccessMessage"] =
            "Se ha iniciado el análisis del historial. Los datos aparecerán en esta página en unos minutos.";

        return RedirectToAction(nameof(Details), new { id = plantId });
    }
}