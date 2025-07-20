using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.DTOs.Common;
using ArandanoIRT.Web._1_Application.DTOs.SensorData;
using ArandanoIRT.Web._1_Application.Services.Contracts;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AmbientDataController : Controller
{
    private readonly IDataQueryService _dataQueryService;
    private readonly IDeviceAdminService _deviceAdminService;
    private readonly ICropService _cropService;
    private readonly IPlantService _plantService;
    private readonly ILogger<AmbientDataController> _logger;

    public AmbientDataController(
        IDataQueryService dataQueryService,
        IDeviceAdminService deviceAdminService,
        ICropService cropService,
        IPlantService plantService,
        ILogger<AmbientDataController> logger)
    {
        _dataQueryService = dataQueryService;
        _deviceAdminService = deviceAdminService;
        _cropService = cropService;
        _plantService = plantService;
        _logger = logger;
    }

    // GET: Admin/AmbientData
    public async Task<IActionResult> Index([FromQuery] DataQueryFilters filters) // Usar [FromQuery] explícitamente
    {
        _logger.LogInformation("Accediendo al listado de datos ambientales con filtros: {FiltersJson}", JsonSerializer.Serialize(filters));

        // Validar y establecer valores por defecto para paginación si no vienen o son inválidos
        filters.PageNumber = filters.PageNumber <= 0 ? 1 : filters.PageNumber;
        filters.PageSize = filters.PageSize switch
        {
            <= 0 => 25, // Default
            > 200 => 200, // Max
            _ => filters.PageSize
        };

        var result = await _dataQueryService.GetSensorDataAsync(filters);

        // Preparar SelectListItems para los dropdowns de filtro
        // Dispositivos
        var devicesResult = await _deviceAdminService.GetAllDevicesAsync();
        ViewBag.AvailableDevices = devicesResult.IsSuccess && devicesResult.Value != null
            ? devicesResult.Value.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Name,
                Selected = d.Id == filters.DeviceId // Establecer 'Selected'
            }).OrderBy(t => t.Text).ToList()
            : new List<SelectListItem> { new SelectListItem("Sin dispositivos", "") };
        // Añadir opción "Todos" al principio si no está ya manejada por el tag helper
        ((List<SelectListItem>)ViewBag.AvailableDevices).Insert(0, new SelectListItem { Value = "", Text = "Todos los Dispositivos", Selected = !filters.DeviceId.HasValue });


        // Plantas
        var plantsResult = await _plantService.GetAllPlantsAsync();
        ViewBag.AvailablePlants = plantsResult.IsSuccess && plantsResult.Value != null
            ? plantsResult.Value.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Name} ({p.CropName})", // Asumiendo que PlantDto tiene CropName
                Selected = p.Id == filters.PlantId // Establecer 'Selected'
            }).OrderBy(t => t.Text).ToList()
            : new List<SelectListItem> { new SelectListItem("Sin plantas", "") };
        ((List<SelectListItem>)ViewBag.AvailablePlants).Insert(0, new SelectListItem { Value = "", Text = "Todas las Plantas", Selected = !filters.PlantId.HasValue });

        // Cultivos
        var cropsResult = await _cropService.GetAllCropsAsync();
        ViewBag.AvailableCrops = cropsResult.IsSuccess && cropsResult.Value != null
            ? cropsResult.Value.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == filters.CropId // Establecer 'Selected'
            }).OrderBy(t => t.Text).ToList()
            : new List<SelectListItem> { new SelectListItem("Sin cultivos", "") };
        ((List<SelectListItem>)ViewBag.AvailableCrops).Insert(0, new SelectListItem { Value = "", Text = "Todos los Cultivos", Selected = !filters.CropId.HasValue });

        // Pasar el objeto de filtros (con valores potencialmente actualizados por defecto) a la vista.
        // Esto es crucial para que el formulario de filtros refleje el estado actual.
        ViewBag.CurrentFilters = filters;

        if (result.IsSuccess && result.Value != null)
        {
            return View(result.Value);
        }

        _logger.LogWarning("Error al obtener datos ambientales: {ErrorMessage}", result.ErrorMessage);
        ViewData["ErrorMessage"] = result.ErrorMessage ?? "Error desconocido al obtener datos.";

        // Devolver una vista con un modelo vacío pero con los filtros para que el formulario funcione
        var emptyPagedResult = new PagedResultDto<SensorDataDisplayDto>
        {
            Items = new List<SensorDataDisplayDto>(),
            PageNumber = filters.PageNumber,
            PageSize = filters.PageSize,
            TotalCount = 0
        };
        return View(emptyPagedResult);
    }
}