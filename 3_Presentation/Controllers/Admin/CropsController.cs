using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CropsController : Controller
{
    private readonly ICropService _cropService;
    private readonly ILogger<CropsController> _logger;

    public CropsController(ICropService cropService, ILogger<CropsController> logger)
    {
        _cropService = cropService;
        _logger = logger;
    }

    // GET: Admin/Crops
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Accediendo al listado de cultivos.");
        var result = await _cropService.GetAllCropsAsync();
        if (result.IsSuccess)
        {
            return View(result.Value);
        }
        // Manejar el error, quizás mostrar un mensaje en la vista
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View(new List<CropSummaryDto>());
    }

    // GET: Admin/Crops/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        _logger.LogInformation("Viendo detalles del cultivo ID: {CropId}", id.Value);
        var result = await _cropService.GetCropByIdAsync(id.Value);
        if (result.IsSuccess)
        {
            if (result.Value == null)
            {
                return NotFound();
            }
            return View(result.Value);
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error"); // O una vista de error específica
    }

    // GET: Admin/Crops/Create
    public IActionResult Create()
    {
        return View(new CropCreateDto());
    }

    // POST: Admin/Crops/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CropCreateDto cropDto)
    {
        if (ModelState.IsValid)
        {
            _logger.LogInformation("Intentando crear cultivo: {CropName}", cropDto.Name);
            var result = await _cropService.CreateCropAsync(cropDto);
            if (result.IsSuccess)
            {
                // Opcional: Añadir un mensaje de éxito a TempData
                TempData["SuccessMessage"] = $"Cultivo '{cropDto.Name}' creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            _logger.LogWarning("Fallo al crear cultivo: {CropName}. Error: {Error}", cropDto.Name, result.ErrorMessage);
        }
        return View(cropDto);
    }

    // GET: Admin/Crops/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        _logger.LogInformation("Editando cultivo ID: {CropId}", id.Value);
        var result = await _cropService.GetCropForEditByIdAsync(id.Value); // Usar el método para DTO de edición
        if (result.IsSuccess)
        {
            if (result.Value == null)
            {
                return NotFound();
            }
            return View(result.Value);
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error");
    }

    // POST: Admin/Crops/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CropEditDto cropDto)
    {
        if (id != cropDto.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            _logger.LogInformation("Intentando actualizar cultivo ID: {CropId}", cropDto.Id);
            var result = await _cropService.UpdateCropAsync(cropDto);
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = $"Cultivo '{cropDto.Name}' actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
             _logger.LogWarning("Fallo al actualizar cultivo ID: {CropId}. Error: {Error}", cropDto.Id, result.ErrorMessage);
        }
        return View(cropDto);
    }

    // GET: Admin/Crops/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        _logger.LogInformation("Confirmando eliminación del cultivo ID: {CropId}", id.Value);
        var result = await _cropService.GetCropByIdAsync(id.Value); // Reusar GetCropByIdAsync para mostrar detalles
        if (result.IsSuccess)
        {
            if (result.Value == null)
            {
                return NotFound();
            }
            return View(result.Value); // Pasar CropDetailsDto a la vista de confirmación de borrado
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error");
    }

    // POST: Admin/Crops/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        _logger.LogInformation("Confirmada eliminación del cultivo ID: {CropId}", id);
        var result = await _cropService.DeleteCropAsync(id);
        if (result.IsSuccess)
        {
             TempData["SuccessMessage"] = $"Cultivo eliminado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        // Si falla, podríamos redirigir a una página de error o a la vista de detalles con un mensaje.
        TempData["ErrorMessage"] = result.ErrorMessage;
        _logger.LogWarning("Fallo al eliminar cultivo ID: {CropId}. Error: {Error}", id, result.ErrorMessage);
        return RedirectToAction(nameof(Delete), new { id = id }); // Volver a la página de confirmación con error
    }
}