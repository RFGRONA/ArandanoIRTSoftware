using ArandanoIRT.Web._1_Application.DTOs.Admin;
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

    // GET: Admin/Devices
    public async Task<IActionResult> Index([FromQuery] DeviceQueryFilters filters)
    {
        var result = await _deviceAdminService.GetPagedDevicesAsync(filters);
        if (result.IsSuccess)
        {
            ViewBag.CurrentFilters = filters;
            ViewBag.AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync();
            ViewBag.AvailableStatuses = _deviceAdminService.GetDeviceStatusesForSelection();
            return View(result.Value);
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View(new ArandanoIRT.Web._1_Application.DTOs.Common.PagedResultDto<DeviceSummaryDto>());
    }

    // GET: Admin/Devices/Details/5
    /// <summary>
    ///     Muestra la página de detalles para un dispositivo específico.
    /// </summary>
    /// <param name="id">El ID del dispositivo a mostrar.</param>
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
    /// <summary>
    ///     Muestra el formulario para crear un nuevo dispositivo.
    /// </summary>
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
            TempData["SuccessMessage"] = $"Dispositivo '{deviceDto.Name}' creado. Código de Activación: {result.Value.ActivationCode}. ID de Dispositivo: {result.Value.DeviceId}";
            return RedirectToAction(nameof(Details), new { id = result.Value.DeviceId });
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage);
        await PopulateDropdownsForDto(deviceDto);
        return View(deviceDto);
    }

    // GET: Admin/Devices/Edit/5
    /// <summary>
    ///     Muestra el formulario para editar un dispositivo existente.
    /// </summary>
    /// <param name="id">El ID del dispositivo a editar.</param>
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
    /// <summary>
    ///     Muestra una vista de confirmación antes de eliminar un dispositivo.
    /// </summary>
    /// <param name="id">El ID del dispositivo a eliminar.</param>
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
    /// <summary>
    ///     Ejecuta la eliminación de un dispositivo tras la confirmación del usuario.
    /// </summary>
    /// <param name="id">El ID del dispositivo a eliminar.</param>
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
    /// <summary>
    ///     Método de utilidad para volver a poblar las listas desplegables en un DTO de creación,
    ///     típicamente después de un error de validación.
    /// </summary>
    private async Task PopulateDropdownsForDto(DeviceCreateDto dto)
    {
        dto.AvailablePlants = await _deviceAdminService.GetPlantsForSelectionAsync();
    }
}