using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ArandanoIRT.Web.Data.DTOs.Admin;
using ArandanoIRT.Web.Services.Contracts;

namespace ArandanoIRT.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DevicesController : Controller
{
    private readonly IDeviceAdminService _deviceAdminService;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(IDeviceAdminService deviceAdminService, ILogger<DevicesController> logger)
    {
        _deviceAdminService = deviceAdminService;
        _logger = logger;
    }

    // GET: Admin/Devices
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Accediendo al listado de dispositivos.");
        var result = await _deviceAdminService.GetAllDevicesAsync();
        if (result.IsSuccess)
        {
            return View(result.Value);
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View(new List<DeviceSummaryDto>());
    }

    // GET: Admin/Devices/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        _logger.LogInformation("Viendo detalles de dispositivo ID: {DeviceId}", id.Value);
        var result = await _deviceAdminService.GetDeviceByIdAsync(id.Value);
        if (result.IsSuccess)
        {
            if (result.Value == null) return NotFound();
            return View(result.Value);
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error");
    }

    // GET: Admin/Devices/Create
    public async Task<IActionResult> Create()
    {
        var dto = new DeviceCreateDto
        {
            AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync(),
            AvailableStatuses = await _deviceAdminService.GetDeviceStatusesForSelectionAsync()
        };
        return View(dto);
    }

    // POST: Admin/Devices/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DeviceCreateDto deviceDto)
    {
        if (!ModelState.IsValid)
        {
            deviceDto.AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync();
            deviceDto.AvailableStatuses = await _deviceAdminService.GetDeviceStatusesForSelectionAsync();
            return View(deviceDto);
        }

        _logger.LogInformation("Intentando crear dispositivo: {DeviceName}", deviceDto.Name);
        var result = await _deviceAdminService.CreateDeviceAsync(deviceDto);
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = $"Dispositivo '{deviceDto.Name}' creado. Código de Activación: {result.Value.ActivationCode} (expira: {result.Value.ActivationCodeExpiresAt:g}). ID de Dispositivo para firmware: {result.Value.DeviceId}";
            return RedirectToAction(nameof(Details), new { id = result.Value.DeviceId });
        }
        ModelState.AddModelError(string.Empty, result.ErrorMessage);
         _logger.LogWarning("Fallo al crear dispositivo: {DeviceName}. Error: {Error}", deviceDto.Name, result.ErrorMessage);
        deviceDto.AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync();
        deviceDto.AvailableStatuses = await _deviceAdminService.GetDeviceStatusesForSelectionAsync();
        return View(deviceDto);
    }

    // GET: Admin/Devices/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        _logger.LogInformation("Editando dispositivo ID: {DeviceId}", id.Value);
        var result = await _deviceAdminService.GetDeviceForEditByIdAsync(id.Value);
        if (result.IsSuccess)
        {
            if (result.Value == null) return NotFound();
            return View(result.Value);
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error");
    }

    // POST: Admin/Devices/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DeviceEditDto deviceDto)
    {
        if (id != deviceDto.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            deviceDto.AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync();
            deviceDto.AvailableStatuses = await _deviceAdminService.GetDeviceStatusesForSelectionAsync();
            return View(deviceDto);
        }

        _logger.LogInformation("Intentando actualizar dispositivo ID: {DeviceId}", deviceDto.Id);
        var result = await _deviceAdminService.UpdateDeviceAsync(deviceDto);
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = $"Dispositivo '{deviceDto.Name}' actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        ModelState.AddModelError(string.Empty, result.ErrorMessage);
        _logger.LogWarning("Fallo al actualizar dispositivo ID: {DeviceId}. Error: {Error}", deviceDto.Id, result.ErrorMessage);
        deviceDto.AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync();
        deviceDto.AvailableStatuses = await _deviceAdminService.GetDeviceStatusesForSelectionAsync();
        return View(deviceDto);
    }

    // GET: Admin/Devices/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
         _logger.LogInformation("Confirmando eliminación de dispositivo ID: {DeviceId}", id.Value);
        var result = await _deviceAdminService.GetDeviceByIdAsync(id.Value);
        if (result.IsSuccess)
        {
            if (result.Value == null) return NotFound();
            return View(result.Value);
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error");
    }

    // POST: Admin/Devices/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        _logger.LogInformation("Confirmada eliminación de dispositivo ID: {DeviceId}", id);
        var result = await _deviceAdminService.DeleteDeviceAsync(id);
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = $"Dispositivo eliminado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        TempData["ErrorMessage"] = result.ErrorMessage;
         _logger.LogWarning("Fallo al eliminar dispositivo ID: {DeviceId}. Error: {Error}", id, result.ErrorMessage);
        return RedirectToAction(nameof(Delete), new { id = id });
    }
}