using System.Text.Json;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Common;
using ArandanoIRT.Web._1_Application.DTOs.SensorData;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize]
public class CapturesController : Controller
{
    private readonly ICropService _cropService;
    private readonly IDataQueryService _dataQueryService;
    private readonly IDeviceAdminService _deviceAdminService;
    private readonly ILogger<CapturesController> _logger;
    private readonly IPlantService _plantService;

    public CapturesController(
        IDataQueryService dataQueryService,
        IDeviceAdminService deviceAdminService,
        ICropService cropService,
        IPlantService plantService,
        ILogger<CapturesController> logger)
    {
        _dataQueryService = dataQueryService;
        _deviceAdminService = deviceAdminService;
        _cropService = cropService;
        _plantService = plantService;
        _logger = logger;
    }

    // GET: Admin/Captures
    public async Task<IActionResult> Index([FromQuery] DataQueryFilters filters)
    {
        _logger.LogInformation("Accediendo al listado de capturas térmicas/RGB con filtros: {FiltersJson}",
            JsonSerializer.Serialize(filters));

        if (!filters.StartDate.HasValue || !filters.EndDate.HasValue)
        {
            filters.EndDate = DateTime.Now;
            filters.StartDate = filters.EndDate.Value.AddDays(-7);
        }

        if (filters.StartDate.HasValue && filters.EndDate.HasValue && filters.StartDate > filters.EndDate)
            (filters.StartDate, filters.EndDate) = (filters.EndDate, filters.StartDate);

        ViewBag.CurrentFilters = filters;

        filters.PageNumber = filters.PageNumber <= 0 ? 1 : filters.PageNumber;
        filters.PageSize = filters.PageSize switch
        {
            <= 0 => 10,
            > 50 => 50,
            _ => filters.PageSize
        };

        var queryFilters = new DataQueryFilters
        {
            DeviceId = filters.DeviceId,
            PlantId = filters.PlantId,
            CropId = filters.CropId,
            PageNumber = filters.PageNumber,
            PageSize = filters.PageSize,
            StartDate = filters.StartDate?.ToSafeUniversalTime(),
            EndDate = filters.EndDate?.Date.AddDays(1).AddTicks(-1).ToSafeUniversalTime()
        };

        var result = await _dataQueryService.GetThermalCapturesAsync(queryFilters);

        var devicesResult = await _deviceAdminService.GetAllDevicesAsync();
        var availableDevices = new List<SelectListItem>();
        if (devicesResult.IsSuccess)
            availableDevices = devicesResult.Value.Select(d => new SelectListItem
            { Value = d.Id.ToString(), Text = d.Name, Selected = d.Id == filters.DeviceId })
                .OrderBy(t => t.Text)
                .ToList();
        availableDevices.Insert(0,
            new SelectListItem { Value = "", Text = "Todos los Dispositivos", Selected = !filters.DeviceId.HasValue });
        ViewBag.AvailableDevices = availableDevices;

        var plantsResult = await _plantService.GetAllPlantsAsync();
        var availablePlants = new List<SelectListItem>();
        if (plantsResult.IsSuccess)
            availablePlants = plantsResult.Value.Select(p => new SelectListItem
            { Value = p.Id.ToString(), Text = $"{p.Name} ({p.CropName})", Selected = p.Id == filters.PlantId })
                .OrderBy(t => t.Text).ToList();
        availablePlants.Insert(0,
            new SelectListItem { Value = "", Text = "Todas las Plantas", Selected = !filters.PlantId.HasValue });
        ViewBag.AvailablePlants = availablePlants;

        var cropsResult = await _cropService.GetAllCropsAsync();
        var availableCrops = new List<SelectListItem>();
        if (cropsResult.IsSuccess)
            availableCrops = cropsResult.Value.Select(c => new SelectListItem
            { Value = c.Id.ToString(), Text = c.Name, Selected = c.Id == filters.CropId }).OrderBy(t => t.Text)
                .ToList();
        availableCrops.Insert(0,
            new SelectListItem { Value = "", Text = "Todos los Cultivos", Selected = !filters.CropId.HasValue });
        ViewBag.AvailableCrops = availableCrops;

        ViewBag.CurrentFilters = filters;

        if (result.IsSuccess) return View(result.Value);

        _logger.LogWarning("Error al obtener capturas: {ErrorMessage}", result.ErrorMessage);
        ViewData["ErrorMessage"] = result.ErrorMessage;

        var emptyResult = new PagedResultDto<ThermalCaptureSummaryDto>
        {
            Items = new List<ThermalCaptureSummaryDto>(),
            PageNumber = filters.PageNumber,
            PageSize = filters.PageSize,
            TotalCount = 0
        };
        return View(emptyResult);
    }

    // GET: Admin/Captures/Details/5
    public async Task<IActionResult> Details(long? id)
    {
        if (id == null)
        {
            _logger.LogWarning("Se solicitó Details de captura con ID nulo.");
            return NotFound();
        }

        _logger.LogInformation("Viendo detalles de captura ID: {CaptureId}", id.Value);
        var result = await _dataQueryService.GetThermalCaptureDetailsAsync(id.Value);

        if (result.IsSuccess)
        {
            if (result.Value == null)
            {
                _logger.LogWarning("No se encontró captura con ID {CaptureId} para Details.", id.Value);
                TempData["ErrorMessage"] = $"No se encontró la captura con ID {id.Value}.";
                return RedirectToAction(nameof(Index));
            }

            return View(result.Value);
        }

        _logger.LogError("Error al obtener detalles de captura ID {CaptureId}: {ErrorMessage}", id.Value,
            result.ErrorMessage);
        TempData["ErrorMessage"] =
            $"No se pudieron cargar los detalles de la captura ID {id.Value}: {result.ErrorMessage}";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> DownloadCsv([FromQuery] DataQueryFilters filters)
    {
        _logger.LogInformation("Iniciando descarga CSV de capturas térmicas con filtros: {FiltersJson}",
            JsonSerializer.Serialize(filters));

        if (filters.StartDate.HasValue && filters.EndDate.HasValue && filters.StartDate > filters.EndDate)
            (filters.StartDate, filters.EndDate) = (filters.EndDate, filters.StartDate);

        if (filters.StartDate.HasValue) filters.StartDate = filters.StartDate.Value.ToSafeUniversalTime();
        if (filters.EndDate.HasValue)
            filters.EndDate = filters.EndDate.Value.Date.AddDays(1).AddTicks(-1).ToSafeUniversalTime();

        var csvBytes = await _dataQueryService.GetThermalCapturesAsCsvAsync(filters);
        var fileName = $"capturas_termicas_{DateTime.Now:yyyyMMddHHmmss}.csv";

        return File(csvBytes, "text/csv", fileName);
    }
}