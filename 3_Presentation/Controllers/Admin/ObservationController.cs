using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Observations;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

/// <summary>
///     Controlador para la gestión de las observaciones manuales realizadas por los usuarios.
/// </summary>
[Area("Admin")]
[Authorize]
public class ObservationController : Controller
{
    private readonly IObservationService _observationService;
    private readonly IPlantService _plantService;
    private readonly IUserService _userService;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="ObservationController" />.
    /// </summary>
    public ObservationController(IObservationService observationService, IPlantService plantService,
        IUserService userService)
    {
        _observationService = observationService;
        _plantService = plantService;
        _userService = userService;
    }

    /// <summary>
    ///     Muestra la página principal con una lista paginada y filtrable de todas las observaciones manuales.
    /// </summary>
    /// <param name="filters">Objeto que contiene los parámetros de filtrado y paginación desde la URL.</param>
    /// <returns>La vista `Index` con los datos paginados y las listas para los filtros.</returns>
    public async Task<IActionResult> Index([FromQuery] ObservationQueryFilters filters)
    {
        if (filters.StartDate.HasValue) filters.StartDate = filters.StartDate.Value.ToSafeUniversalTime();
        if (filters.EndDate.HasValue)
            filters.EndDate = filters.EndDate.Value.Date.AddDays(1).AddTicks(-1).ToSafeUniversalTime();

        var result = await _observationService.GetPagedObservationsAsync(filters);

        ViewBag.AvailablePlants = await _plantService.GetPlantsForSelectionAsync();
        ViewBag.AvailableUsers = await _userService.GetUsersForSelectionAsync();
        ViewBag.CurrentFilters = filters;

        return View(result);
    }

    /// <summary>
    ///     Muestra el formulario para crear una nueva observación manual.
    /// </summary>
    public async Task<IActionResult> Create()
    {
        var model = new ObservationCreateDto
        {
            AvailablePlants = await _plantService.GetPlantsForSelectionAsync()
        };
        return View(model);
    }

    /// <summary>
    ///     Procesa el envío del formulario para crear una nueva observación.
    /// </summary>
    /// <param name="model">Los datos de la nueva observación a crear.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ObservationCreateDto model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailablePlants = await _plantService.GetPlantsForSelectionAsync();
            return View(model);
        }

        var result = await _observationService.CreateObservationAsync(model, User);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Observación registrada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.ErrorMessage);
        model.AvailablePlants = await _plantService.GetPlantsForSelectionAsync();
        return View(model);
    }
}