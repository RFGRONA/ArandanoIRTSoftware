using ArandanoIRT.Web._1_Application.DTOs.Plants;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize]
public class PlantsController : BaseAdminController
{
    private readonly IPlantService _plantService;

    public PlantsController(IPlantService plantService)
    {
        _plantService = plantService;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _plantService.GetAllPlantsAsync();
        if (!result.IsSuccess)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage;
            return View(new List<PlantSummaryDto>());
        }

        return View(result.Value);
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[ErrorMessageKey] = InvalidRequestDataMessage;
            return RedirectToAction(nameof(Index));
        }

        var result = await _plantService.GetPlantByIdAsync(id);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage ?? "Plant not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    // CORREGIDO: Instanciar y poblar el DTO para la vista.
    public async Task<IActionResult> Create()
    {
        var model = new PlantCreateDto
        {
            AvailableCrops = await _plantService.GetCropsForSelectionAsync(),
            AvailableExperimentalGroups = _plantService.GetExperimentalGroupsForSelection()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlantCreateDto plantDto)
    {
        // CORREGIDO: Si el modelo no es válido, repoblar la lista de cultivos en el DTO.
        if (!ModelState.IsValid)
        {
            plantDto.AvailableCrops = await _plantService.GetCropsForSelectionAsync();
            plantDto.AvailableExperimentalGroups = _plantService.GetExperimentalGroupsForSelection();
            return View(plantDto);
        }

        var result = await _plantService.CreatePlantAsync(plantDto);

        if (!result.IsSuccess)
        {
            plantDto.AvailableCrops = await _plantService.GetCropsForSelectionAsync();
            plantDto.AvailableExperimentalGroups = _plantService.GetExperimentalGroupsForSelection();
        }
        return HandleServiceResult(result, nameof(Index), plantDto);
    }

    // CORREGIDO: La lógica que poblaba el ViewBag era redundante, el servicio ya lo hace.
    public async Task<IActionResult> Edit(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[ErrorMessageKey] = InvalidRequestDataMessage;
            return RedirectToAction(nameof(Index));
        }

        var result = await _plantService.GetPlantForEditByIdAsync(id);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage ?? "Plant not found.";
            return RedirectToAction(nameof(Index));
        }

        // El DTO (result.Value) ya viene con AvailableCrops poblado desde el servicio.
        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PlantEditDto plantDto)
    {
        if (id != plantDto.Id) return BadRequest();

        // CORREGIDO: Si el modelo no es válido, repoblar la lista de cultivos.
        if (!ModelState.IsValid)
        {
            plantDto.AvailableCrops = await _plantService.GetCropsForSelectionAsync();
            plantDto.AvailableExperimentalGroups = _plantService.GetExperimentalGroupsForSelection();
            return View(plantDto);
        }

        var result = await _plantService.UpdatePlantAsync(plantDto);

        if (!result.IsSuccess)
        {
            plantDto.AvailableCrops = await _plantService.GetCropsForSelectionAsync();
            plantDto.AvailableExperimentalGroups = _plantService.GetExperimentalGroupsForSelection();
        }
        return HandleServiceResult(result, nameof(Index), plantDto);
    }

    public async Task<IActionResult> Delete(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[ErrorMessageKey] = InvalidRequestDataMessage;
            return RedirectToAction(nameof(Index));
        }

        var result = await _plantService.GetPlantByIdAsync(id);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage ?? "Plant not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[ErrorMessageKey] = InvalidRequestDataMessage;
            return RedirectToAction(nameof(Index));
        }

        var result = await _plantService.DeletePlantAsync(id);
        return HandleServiceResult(result, nameof(Index), nameof(Delete));
    }
}