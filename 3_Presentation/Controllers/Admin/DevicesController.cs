using ArandanoIRT.Web._1_Application.DTOs.Device;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

/// <summary>
///     Controlador para la gestión CRUD (Crear, Leer, Actualizar, Eliminar) de las entidades de Dispositivo.
///     El acceso a este controlador está restringido a usuarios con el rol "Admin".
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DevicesController : Controller
{
    private readonly IDeviceAdminService _deviceAdminService;
    private readonly ILogger<DevicesController> _logger;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="DevicesController" />.
    /// </summary>
    public DevicesController(IDeviceAdminService deviceAdminService, ILogger<DevicesController> logger)
    {
        _deviceAdminService = deviceAdminService;
        _logger = logger;
    }

    /// <summary>
    ///     Muestra la página principal con una lista de todos los dispositivos.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var result = await _deviceAdminService.GetAllDevicesAsync();
        if (result.IsSuccess) return View(result.Value);
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View(new List<DeviceSummaryDto>());
    }

    /// <summary>
    ///     Muestra la página de detalles para un dispositivo específico.
    /// </summary>
    /// <param name="id">El ID del dispositivo a mostrar.</param>
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var result = await _deviceAdminService.GetDeviceByIdAsync(id.Value);
        if (result.IsSuccess) return result.Value == null ? NotFound() : View(result.Value);
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error");
    }

    /// <summary>
    ///     Muestra el formulario para crear un nuevo dispositivo.
    /// </summary>
    public async Task<IActionResult> Create()
    {
        var dto = new DeviceCreateDto
        {
            AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync()
        };
        return View(dto);
    }

    /// <summary>
    ///     Procesa el envío del formulario para crear un nuevo dispositivo.
    /// </summary>
    /// <param name="deviceDto">Los datos del nuevo dispositivo a crear.</param>
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
            TempData["SuccessMessage"] =
                $"Dispositivo '{deviceDto.Name}' creado. Código de Activación: {result.Value.ActivationCode}. ID de Dispositivo: {result.Value.DeviceId}";
            return RedirectToAction(nameof(Details), new { id = result.Value.DeviceId });
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage);
        await PopulateDropdownsForDto(deviceDto);
        return View(deviceDto);
    }

    /// <summary>
    ///     Muestra el formulario para editar un dispositivo existente.
    /// </summary>
    /// <param name="id">El ID del dispositivo a editar.</param>
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var result = await _deviceAdminService.GetDeviceForEditByIdAsync(id.Value);
        if (result.IsSuccess) return result.Value == null ? NotFound() : View(result.Value);
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error");
    }

    /// <summary>
    ///     Procesa el envío del formulario para actualizar un dispositivo existente.
    /// </summary>
    /// <param name="id">El ID del dispositivo que se está editando.</param>
    /// <param name="deviceDto">Los datos actualizados del dispositivo.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DeviceEditDto deviceDto)
    {
        if (id != deviceDto.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            deviceDto.AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync();
            deviceDto.AvailableStatuses = _deviceAdminService.GetDeviceStatusesForSelection();
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
        deviceDto.AvailableStatuses = _deviceAdminService.GetDeviceStatusesForSelection();
        return View(deviceDto);
    }

    /// <summary>
    ///     Muestra una vista de confirmación antes de eliminar un dispositivo.
    /// </summary>
    /// <param name="id">El ID del dispositivo a eliminar.</param>
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var result = await _deviceAdminService.GetDeviceByIdAsync(id.Value);
        if (result.IsSuccess) return result.Value == null ? NotFound() : View(result.Value);
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error");
    }

    /// <summary>
    ///     Ejecuta la eliminación de un dispositivo tras la confirmación del usuario.
    /// </summary>
    /// <param name="id">El ID del dispositivo a eliminar.</param>
    [HttpPost]
    [ActionName("Delete")]
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
        return RedirectToAction(nameof(Delete), new { id });
    }

    /// <summary>
    ///     Método de utilidad para volver a poblar las listas desplegables en un DTO de creación,
    ///     típicamente después de un error de validación.
    /// </summary>
    private async Task PopulateDropdownsForDto(DeviceCreateDto dto)
    {
        dto.AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync();
    }
}