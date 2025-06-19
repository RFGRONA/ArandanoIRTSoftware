using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ArandanoIRT.Web.Data.DTOs.Admin;
using ArandanoIRT.Web.Services.Contracts;

namespace ArandanoIRT.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PlantsController : Controller
{
    private readonly IPlantService _plantService;
    private readonly ILogger<PlantsController> _logger;

    public PlantsController(IPlantService plantService, ILogger<PlantsController> logger)
    {
        _plantService = plantService;
        _logger = logger;
    }

    // GET: Admin/Plants
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Accediendo al listado de plantas.");
        var result = await _plantService.GetAllPlantsAsync();
        if (result.IsSuccess)
        {
            return View(result.Value);
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View(new List<PlantSummaryDto>());
    }

    // GET: Admin/Plants/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        _logger.LogInformation("Viendo detalles de planta ID: {PlantId}", id.Value);
        var result = await _plantService.GetPlantByIdAsync(id.Value);
        if (result.IsSuccess)
        {
            if (result.Value == null) return NotFound();
            return View(result.Value);
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error");
    }

    // GET: Admin/Plants/Create
    public async Task<IActionResult> Create()
    {
        var dto = new PlantCreateDto
        {
            AvailableCrops = await _plantService.GetCropsForSelectionAsync(),
            AvailableStatuses = await _plantService.GetStatusesForSelectionAsync()
        };
        return View(dto);
    }

    // POST: Admin/Plants/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlantCreateDto plantDto)
    {
        // Repopular listas si ModelState es inválido
        if (!ModelState.IsValid)
        {
            plantDto.AvailableCrops = await _plantService.GetCropsForSelectionAsync();
            plantDto.AvailableStatuses = await _plantService.GetStatusesForSelectionAsync();
            return View(plantDto);
        }

        _logger.LogInformation("Intentando crear planta: {PlantName}", plantDto.Name);
        var result = await _plantService.CreatePlantAsync(plantDto);
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = $"Planta '{plantDto.Name}' creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        ModelState.AddModelError(string.Empty, result.ErrorMessage);
        _logger.LogWarning("Fallo al crear planta: {PlantName}. Error: {Error}", plantDto.Name, result.ErrorMessage);
        plantDto.AvailableCrops = await _plantService.GetCropsForSelectionAsync();
        plantDto.AvailableStatuses = await _plantService.GetStatusesForSelectionAsync();
        return View(plantDto);
    }

    // GET: Admin/Plants/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        _logger.LogInformation("Editando planta ID: {PlantId}", id.Value);
        var result = await _plantService.GetPlantForEditByIdAsync(id.Value);
        if (result.IsSuccess)
        {
            if (result.Value == null) return NotFound();
            // GetPlantForEditByIdAsync ya debería poblar AvailableCrops y AvailableStatuses
            return View(result.Value);
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error");
    }

    // POST: Admin/Plants/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PlantEditDto plantDto)
    {
        if (id != plantDto.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            plantDto.AvailableCrops = await _plantService.GetCropsForSelectionAsync();
            plantDto.AvailableStatuses = await _plantService.GetStatusesForSelectionAsync();
            return View(plantDto);
        }

        _logger.LogInformation("Intentando actualizar planta ID: {PlantId}", plantDto.Id);
        var result = await _plantService.UpdatePlantAsync(plantDto);
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = $"Planta '{plantDto.Name}' actualizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        ModelState.AddModelError(string.Empty, result.ErrorMessage);
        _logger.LogWarning("Fallo al actualizar planta ID: {PlantId}. Error: {Error}", plantDto.Id, result.ErrorMessage);
        plantDto.AvailableCrops = await _plantService.GetCropsForSelectionAsync();
        plantDto.AvailableStatuses = await _plantService.GetStatusesForSelectionAsync();
        return View(plantDto);
    }

    // GET: Admin/Plants/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        _logger.LogInformation("Confirmando eliminación de planta ID: {PlantId}", id.Value);
        var result = await _plantService.GetPlantByIdAsync(id.Value);
        if (result.IsSuccess)
        {
            if (result.Value == null) return NotFound();
            return View(result.Value);
        }
        ViewData["ErrorMessage"] = result.ErrorMessage;
        return View("Error");
    }

    // POST: Admin/Plants/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        _logger.LogInformation("Confirmada eliminación de planta ID: {PlantId}", id);
        var result = await _plantService.DeletePlantAsync(id);
        if (result.IsSuccess)
        {
             TempData["SuccessMessage"] = $"Planta eliminada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        TempData["ErrorMessage"] = result.ErrorMessage;
        _logger.LogWarning("Fallo al eliminar planta ID: {PlantId}. Error: {Error}", id, result.ErrorMessage);
        return RedirectToAction(nameof(Delete), new { id = id });
    }
}