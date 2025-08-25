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
public class AmbientDataController : Controller
{
    private readonly ICropService _cropService;
    private readonly IDataQueryService _dataQueryService;
    private readonly IDeviceAdminService _deviceAdminService;
    private readonly ILogger<AmbientDataController> _logger;
    private readonly IPlantService _plantService;

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
    public async Task<IActionResult> Index([FromQuery] DataQueryFilters filters)
    {
        _logger.LogInformation("Accediendo al listado de datos ambientales con filtros: {FiltersJson}",
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
            <= 0 => 25,
            > 200 => 200,
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

        var result = await _dataQueryService.GetSensorDataAsync(queryFilters);

        var devicesResult = await _deviceAdminService.GetAllDevicesAsync();
        ViewBag.AvailableDevices = devicesResult.IsSuccess && devicesResult.Value != null
            ? devicesResult.Value.Select(d => new SelectListItem
            { Value = d.Id.ToString(), Text = d.Name, Selected = d.Id == filters.DeviceId })
                .OrderBy(t => t.Text)
                .ToList()
            : new List<SelectListItem> { new("Sin dispositivos", "") };
        ((List<SelectListItem>)ViewBag.AvailableDevices).Insert(0,
            new SelectListItem { Value = "", Text = "Todos los Dispositivos", Selected = !filters.DeviceId.HasValue });

        var plantsResult = await _plantService.GetAllPlantsAsync();
        ViewBag.AvailablePlants = plantsResult.IsSuccess && plantsResult.Value != null
            ? plantsResult.Value.Select(p => new SelectListItem
            { Value = p.Id.ToString(), Text = $"{p.Name} ({p.CropName})", Selected = p.Id == filters.PlantId })
                .OrderBy(t => t.Text).ToList()
            : new List<SelectListItem> { new("Sin plantas", "") };
        ((List<SelectListItem>)ViewBag.AvailablePlants).Insert(0,
            new SelectListItem { Value = "", Text = "Todas las Plantas", Selected = !filters.PlantId.HasValue });

        var cropsResult = await _cropService.GetAllCropsAsync();
        ViewBag.AvailableCrops = cropsResult.IsSuccess && cropsResult.Value != null
            ? cropsResult.Value.Select(c => new SelectListItem
            { Value = c.Id.ToString(), Text = c.Name, Selected = c.Id == filters.CropId }).OrderBy(t => t.Text)
                .ToList()
            : new List<SelectListItem> { new("Sin cultivos", "") };
        ((List<SelectListItem>)ViewBag.AvailableCrops).Insert(0,
            new SelectListItem { Value = "", Text = "Todos los Cultivos", Selected = !filters.CropId.HasValue });

        ViewBag.CurrentFilters = filters;

        if (result.IsSuccess && result.Value != null) return View(result.Value);

        _logger.LogWarning("Error al obtener datos ambientales: {ErrorMessage}", result.ErrorMessage);
        ViewData["ErrorMessage"] = result.ErrorMessage ?? "Error desconocido al obtener datos.";

        var emptyPagedResult = new PagedResultDto<SensorDataDisplayDto>
        {
            Items = new List<SensorDataDisplayDto>(),
            PageNumber = filters.PageNumber,
            PageSize = filters.PageSize,
            TotalCount = 0
        };
        return View(emptyPagedResult);
    }

    public async Task<IActionResult> DownloadCsv([FromQuery] DataQueryFilters filters)
    {
        _logger.LogInformation("Iniciando descarga CSV de datos ambientales con filtros: {FiltersJson}",
            JsonSerializer.Serialize(filters));

        if (filters.StartDate.HasValue && filters.EndDate.HasValue && filters.StartDate > filters.EndDate)
            (filters.StartDate, filters.EndDate) = (filters.EndDate, filters.StartDate);

        // Es importante aplicar la misma lógica de fechas que en la acción Index
        if (filters.StartDate.HasValue) filters.StartDate = filters.StartDate.Value.ToSafeUniversalTime();
        if (filters.EndDate.HasValue)
            filters.EndDate = filters.EndDate.Value.Date.AddDays(1).AddTicks(-1).ToSafeUniversalTime();

        // Llamamos al método del servicio que ya creamos
        var csvBytes = await _dataQueryService.GetAmbientDataAsCsvAsync(filters);

        // Creamos un nombre de archivo dinámico con la fecha
        var fileName = $"datos_ambientales_{DateTime.Now:yyyyMMddHHmmss}.csv";

        // Devolvemos el archivo al navegador para que inicie la descarga
        return File(csvBytes, "text/csv", fileName);
    }
}