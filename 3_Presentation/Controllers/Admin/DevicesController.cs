using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

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
        var result = await _deviceAdminService.GetDeviceByIdAsync(id.Value);
        if (result.IsSuccess)
        {
            // Pequeño ajuste para que el DTO de detalles coincida con el nombre en la vista
            // El DTO de detalles tiene "ActivationDevices", pero el servicio lo llena en "ActivationInfo"
            // Asegurarse que el servicio `GetDeviceByIdAsync` llene la propiedad correcta del DTO.
            // Asumiendo que `DeviceDetailsDto.ActivationDevices` es la propiedad correcta:
            return result.Value == null ? NotFound() : View(result.Value);
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error");
    }

    // GET: Admin/Devices/Create
    public async Task<IActionResult> Create()
    {
        var dto = new DeviceCreateDto
        {
            // Llama a los helpers del servicio que ahora tienen la lógica correcta.
            AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync()
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
            await PopulateDropdownsForDto(deviceDto);
            return View(deviceDto);
        }

        var result = await _deviceAdminService.CreateDeviceAsync(deviceDto);
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = $"Dispositivo '{deviceDto.Name}' creado. Código de Activación: {result.Value.ActivationCode}. ID de Dispositivo: {result.Value.DeviceId}";
            return RedirectToAction(nameof(Details), new { id = result.Value.DeviceId });
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage);
        await PopulateDropdownsForDto(deviceDto);
        return View(deviceDto);
    }

    // GET: Admin/Devices/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var result = await _deviceAdminService.GetDeviceForEditByIdAsync(id.Value);
        if (result.IsSuccess)
        {
            return result.Value == null ? NotFound() : View(result.Value);
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
            // Si la validación falla, volvemos a poblar los dropdowns
            deviceDto.AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync();
            deviceDto.AvailableStatuses = _deviceAdminService.GetDeviceStatusesForSelection().Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Text = s.Text, Value = s.Value });
            return View(deviceDto);
        }

        var result = await _deviceAdminService.UpdateDeviceAsync(deviceDto);
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Dispositivo actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage);
        deviceDto.AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync();
        deviceDto.AvailableStatuses = _deviceAdminService.GetDeviceStatusesForSelection().Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Text = s.Text, Value = s.Value });
        return View(deviceDto);
    }

    // GET: Admin/Devices/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var result = await _deviceAdminService.GetDeviceByIdAsync(id.Value);
        if (result.IsSuccess)
        {
            return result.Value == null ? NotFound() : View(result.Value);
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error");
    }

    // POST: Admin/Devices/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _deviceAdminService.DeleteDeviceAsync(id);
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Dispositivo eliminado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        TempData["ErrorMessage"] = result.ErrorMessage;
        return RedirectToAction(nameof(Delete), new { id = id });
    }

    // --- MÉTODOS AUXILIARES ---

    // Método helper para poblar los dropdowns en caso de error de validación
    private async Task PopulateDropdownsForDto(DeviceCreateDto dto)
    {
        dto.AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync();
    }
}