using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.DTOs.Crops;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

/// <summary>
///     Controlador para la gestión CRUD (Crear, Leer, Actualizar, Eliminar) de las entidades de Cultivo.
///     Requiere autorización y pertenece al área de Administración.
/// </summary>
[Area("Admin")]
[Authorize]
public class CropsController : BaseAdminController
{
    private readonly ICropService _cropService;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="CropsController" />.
    /// </summary>
    public CropsController(ICropService cropService)
    {
        _cropService = cropService;
    }

    // Corrected to use the actual service method
    public async Task<IActionResult> Index([FromQuery] CropQueryFilters filters)
    {
        var result = await _cropService.GetPagedCropsAsync(filters);
        if (!result.IsSuccess)
        {
            // Handle the case where the list cannot be retrieved
            TempData[ErrorMessageKey] = result.ErrorMessage;
            return View(new ArandanoIRT.Web._1_Application.DTOs.Common.PagedResultDto<CropSummaryDto>());
        }

        ViewBag.CurrentFilters = filters;
        return View(result.Value);
    }

    // Corrected to use the actual service method
    /// <summary>
    ///     Muestra la página de detalles para un cultivo específico.
    /// </summary>
    /// <param name="id">El ID del cultivo a mostrar.</param>
    public async Task<IActionResult> Details(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[ErrorMessageKey] = InvalidRequestDataMessage;
            return RedirectToAction(nameof(Index));
        }
        var result = await _cropService.GetCropByIdAsync(id);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage ?? "Crop not found.";
            return RedirectToAction(nameof(Index));
        }
        return View(result.Value);
    }

    /// <summary>
    ///     Muestra el formulario para crear un nuevo cultivo.
    /// </summary>
    public IActionResult Create()
    {
        var model = new CropCreateDto();
        return View(model);
    }

    /// <summary>
    ///     Procesa el envío del formulario para crear un nuevo cultivo.
    /// </summary>
    /// <param name="cropCreateDto">Los datos del nuevo cultivo a crear.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CropCreateDto cropCreateDto)
    {
        if (!ModelState.IsValid)
        {
            return View(cropCreateDto);
        }
        var result = await _cropService.CreateCropAsync(cropCreateDto);
        return HandleServiceResult(result, nameof(Index), cropCreateDto);
    }

    // Corrected to use the actual service method
    /// <summary>
    ///     Muestra el formulario para editar un cultivo existente.
    /// </summary>
    /// <param name="id">El ID del cultivo a editar.</param>
    public async Task<IActionResult> Edit(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[ErrorMessageKey] = InvalidRequestDataMessage;
            return RedirectToAction(nameof(Index));
        }
        var result = await _cropService.GetCropForEditByIdAsync(id);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage ?? "Crop not found.";
            return RedirectToAction(nameof(Index));
        }
        return View(result.Value);
    }

    /// <summary>
    ///     Procesa el envío del formulario para actualizar un cultivo existente.
    /// </summary>
    /// <param name="id">El ID del cultivo que se está editando.</param>
    /// <param name="cropEditDto">Los datos actualizados del cultivo.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CropEditDto cropEditDto)
    {
        if (id != cropEditDto.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(cropEditDto);
        }

        var result = await _cropService.UpdateCropAsync(cropEditDto);
        return HandleServiceResult(result, nameof(Index), cropEditDto);
    }

    // Show delete confirmation view for a crop
    /// <summary>
    ///     Muestra una vista de confirmación antes de eliminar un cultivo.
    /// </summary>
    /// <param name="id">El ID del cultivo a eliminar.</param>
    public async Task<IActionResult> Delete(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[ErrorMessageKey] = InvalidRequestDataMessage;
            return RedirectToAction(nameof(Index));
        }
        var result = await _cropService.GetCropByIdAsync(id);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage ?? "Crop not found.";
            return RedirectToAction(nameof(Index));
        }
        // Optionally, pass a flag or message to indicate this is a delete confirmation
        ViewBag.IsDeleteConfirmation = true;
        return View(result.Value);
    }

    /// <summary>
    ///     Ejecuta la eliminación de un cultivo tras la confirmación del usuario.
    /// </summary>
    /// <param name="id">El ID del cultivo a eliminar.</param>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[ErrorMessageKey] = InvalidRequestDataMessage;
            return RedirectToAction(nameof(Delete), new { id });
        }

        // The service method name is correct here
        var result = await _cropService.DeleteCropAsync(id);
        return HandleServiceResult(result, nameof(Index), nameof(Delete));
    }
}