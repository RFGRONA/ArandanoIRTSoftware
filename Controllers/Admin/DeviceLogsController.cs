using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
// Para PagedResultDto, DataQueryFilters, DeviceSummaryDto
using System.Text.Json;
using ArandanoIRT.Web.Data.DTOs.Admin;
using ArandanoIRT.Web.Services.Contracts; // Para logging

namespace ArandanoIRT.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DeviceLogsController : Controller
{
    private readonly IDataQueryService _dataQueryService;
    private readonly IDeviceAdminService _deviceAdminService;
    private readonly ILogger<DeviceLogsController> _logger;

    public DeviceLogsController(
        IDataQueryService dataQueryService,
        IDeviceAdminService deviceAdminService,
        ILogger<DeviceLogsController> logger)
    {
        _dataQueryService = dataQueryService;
        _deviceAdminService = deviceAdminService;
        _logger = logger;
    }

    // GET: Admin/DeviceLogs
    public async Task<IActionResult> Index([FromQuery] DataQueryFilters filters)
    {
        _logger.LogInformation("Accediendo al listado de logs de dispositivo con filtros: {FiltersJson}", JsonSerializer.Serialize(filters));

        filters.PageNumber = filters.PageNumber <= 0 ? 1 : filters.PageNumber;
        filters.PageSize = filters.PageSize switch
        {
            <= 0 => 25, // Default, ajustado de 10 a 25 como en AmbientData
            > 100 => 100, // Max
            _ => filters.PageSize
        };

        var result = await _dataQueryService.GetDeviceLogsAsync(filters);

        // Para los filtros en la vista: Dispositivos
        var devicesResult = await _deviceAdminService.GetAllDevicesAsync();
        var availableDevicesForFilter = new List<SelectListItem>();
        if (devicesResult.IsSuccess && devicesResult.Value != null)
        {
            availableDevicesForFilter = devicesResult.Value
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name,
                    Selected = d.Id == filters.DeviceId // Establecer 'Selected'
                })
                .OrderBy(t => t.Text)
                .ToList();
        }
        else
        {
            _logger.LogWarning("No se pudieron obtener los dispositivos para el filtro: {Error}", devicesResult.ErrorMessage);
        }
        availableDevicesForFilter.Insert(0, new SelectListItem { Value = "", Text = "Todos los Dispositivos", Selected = !filters.DeviceId.HasValue });
        ViewBag.AvailableDevicesForFilter = availableDevicesForFilter;

        // Para los filtros en la vista: Niveles de Log
        var availableLogLevels = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "Todos los Tipos", Selected = string.IsNullOrEmpty(filters.LogLevel) },
            new SelectListItem { Value = "INFO", Text = "INFO", Selected = "INFO".Equals(filters.LogLevel, StringComparison.OrdinalIgnoreCase) },
            new SelectListItem { Value = "WARNING", Text = "WARNING", Selected = "WARNING".Equals(filters.LogLevel, StringComparison.OrdinalIgnoreCase) },
            new SelectListItem { Value = "ERROR", Text = "ERROR", Selected = "ERROR".Equals(filters.LogLevel, StringComparison.OrdinalIgnoreCase) }
            // Añade más si tienes otros tipos de log y asegúrate de marcar 'Selected' correctamente
        };
        ViewBag.AvailableLogLevels = availableLogLevels;
        
        ViewBag.CurrentFilters = filters; // Pasar los filtros actuales a la vista

        if (result.IsSuccess && result.Value != null)
        {
            return View(result.Value);
        }

        _logger.LogWarning("Error al obtener logs de dispositivo: {ErrorMessage}", result.ErrorMessage);
        ViewData["ErrorMessage"] = result.ErrorMessage ?? "Error desconocido al obtener logs.";
        
        var emptyResult = new PagedResultDto<DeviceLogDisplayDto>
        {
            Items = new List<DeviceLogDisplayDto>(),
            PageNumber = filters.PageNumber,
            PageSize = filters.PageSize,
            TotalCount = 0
        };
        return View(emptyResult);
    }
}