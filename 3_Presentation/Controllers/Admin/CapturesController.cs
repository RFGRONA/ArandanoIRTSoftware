using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CapturesController : Controller
{
    private readonly IDataQueryService _dataQueryService;
    private readonly IDeviceAdminService _deviceAdminService;
    private readonly ICropService _cropService;
    private readonly IPlantService _plantService;
    private readonly ILogger<CapturesController> _logger;

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
        _logger.LogInformation("Accediendo al listado de capturas térmicas/RGB con filtros: {FiltersJson}", JsonSerializer.Serialize(filters));

        filters.PageNumber = filters.PageNumber <= 0 ? 1 : filters.PageNumber;
        filters.PageSize = filters.PageSize switch
        {
            <= 0 => 10, // Default
            > 50 => 50,  // Max
            _ => filters.PageSize
        };

        var result = await _dataQueryService.GetThermalCapturesAsync(filters);

        // Dispositivos
        var devicesResult = await _deviceAdminService.GetAllDevicesAsync();
        var availableDevices = new List<SelectListItem>();
        if (devicesResult.IsSuccess && devicesResult.Value != null)
        {
            availableDevices = devicesResult.Value.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Name,
                Selected = d.Id == filters.DeviceId
            }).OrderBy(t => t.Text).ToList();
        }
        availableDevices.Insert(0, new SelectListItem { Value = "", Text = "Todos los Dispositivos", Selected = !filters.DeviceId.HasValue });
        ViewBag.AvailableDevices = availableDevices;

        // Plantas
        var plantsResult = await _plantService.GetAllPlantsAsync();
        var availablePlants = new List<SelectListItem>();
        if (plantsResult.IsSuccess && plantsResult.Value != null)
        {
            availablePlants = plantsResult.Value.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Name} ({p.CropName})", // Asume que PlantDto tiene CropName
                Selected = p.Id == filters.PlantId
            }).OrderBy(t => t.Text).ToList();
        }
        availablePlants.Insert(0, new SelectListItem { Value = "", Text = "Todas las Plantas", Selected = !filters.PlantId.HasValue });
        ViewBag.AvailablePlants = availablePlants;

        // Cultivos
        var cropsResult = await _cropService.GetAllCropsAsync();
        var availableCrops = new List<SelectListItem>();
        if (cropsResult.IsSuccess && cropsResult.Value != null)
        {
            availableCrops = cropsResult.Value.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == filters.CropId
            }).OrderBy(t => t.Text).ToList();
        }
        availableCrops.Insert(0, new SelectListItem { Value = "", Text = "Todos los Cultivos", Selected = !filters.CropId.HasValue });
        ViewBag.AvailableCrops = availableCrops;

        ViewBag.CurrentFilters = filters;

        if (result.IsSuccess && result.Value != null)
        {
            return View(result.Value);
        }

        _logger.LogWarning("Error al obtener capturas: {ErrorMessage}", result.ErrorMessage);
        ViewData["ErrorMessage"] = result.ErrorMessage ?? "Error desconocido al obtener capturas.";
        
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
    public async Task<IActionResult> Details(long? id) // El ID de ThermalCaptureSummaryDto y ThermalCaptureDetailsDto es long
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
                return RedirectToAction(nameof(Index)); // Redirigir si no se encuentra
            }
            return View(result.Value);
        }
        
        _logger.LogError("Error al obtener detalles de captura ID {CaptureId}: {ErrorMessage}", id.Value, result.ErrorMessage);
        TempData["ErrorMessage"] = $"No se pudieron cargar los detalles de la captura ID {id.Value}: {result.ErrorMessage}";
        return RedirectToAction(nameof(Index));
    }
}