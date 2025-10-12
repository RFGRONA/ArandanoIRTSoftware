using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Plants;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

/// <summary>
/// Controlador para la gestión CRUD (Crear, Leer, Actualizar, Eliminar) de las entidades de Planta.
/// Requiere autorización y pertenece al área de Administración.
/// </summary>
[Area("Admin")]
[Authorize]
public class PlantsController : BaseAdminController
{
    private readonly IPlantService _plantService;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="PlantsController"/>.
    /// </summary>
    public PlantsController(IPlantService plantService)
    {
        _plantService = plantService;
    }

    /// <summary>
    /// Muestra la página principal con una lista de todas las plantas.
    /// </summary>
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

    /// <summary>
    /// Muestra la página de detalles para una planta específica.
    /// </summary>
    /// <param name="id">El ID de la planta a mostrar.</param>
    public async Task<IActionResult> Details(int id)
    {
        var result = await _plantService.GetPlantByIdAsync(id);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage ?? "Planta no encontrada.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    /// <summary>
    /// Muestra el formulario para crear una nueva planta.
    /// </summary>
    public async Task<IActionResult> Create()
    {
        var model = new PlantCreateDto
        {
            AvailableCrops = await _plantService.GetCropsForSelectionAsync(),
            AvailableExperimentalGroups = _plantService.GetExperimentalGroupsForSelection()
        };
        ViewBag.AvailableStatuses = EnumSelectListExtensions.ToSelectList<PlantStatus>();
        return View(model);
    }

    /// <summary>
    /// Procesa el envío del formulario para crear una nueva planta.
    /// </summary>
    /// <param name="plantDto">Los datos de la nueva planta a crear.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlantCreateDto plantDto)
    {
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

    /// <summary>
    /// Muestra el formulario para editar una planta existente.
    /// </summary>
    /// <param name="id">El ID de la planta a editar.</param>
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _plantService.GetPlantForEditByIdAsync(id);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage ?? "Planta no encontrada.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    /// <summary>
    /// Procesa el envío del formulario para actualizar una planta existente.
    /// </summary>
    /// <param name="id">El ID de la planta que se está editando.</param>
    /// <param name="plantDto">Los datos actualizados de la planta.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PlantEditDto plantDto)
    {
        if (id != plantDto.Id) return BadRequest();

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

    /// <summary>
    /// Muestra una vista de confirmación antes de eliminar una planta.
    /// </summary>
    /// <param name="id">El ID de la planta a eliminar.</param>
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _plantService.GetPlantByIdAsync(id);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage ?? "Planta no encontrada.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    /// <summary>
    /// Ejecuta la eliminación de una planta tras la confirmación del usuario.
    /// </summary>
    /// <param name="id">El ID de la planta a eliminar.</param>
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _plantService.DeletePlantAsync(id);
        return HandleServiceResult(result, nameof(Index), nameof(Delete));
    }
}