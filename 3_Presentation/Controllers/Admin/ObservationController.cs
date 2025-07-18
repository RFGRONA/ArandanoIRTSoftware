using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize] // Proteger todo el controlador
public class ObservationController : Controller
{
    private readonly IObservationService _observationService;
    private readonly IPlantService _plantService;
    private readonly IUserService _userService;

    public ObservationController(IObservationService observationService, IPlantService plantService,
        IUserService userService)
    {
        _observationService = observationService;
        _plantService = plantService;
        _userService = userService;
    }

    // GET: Admin/Observation
    public async Task<IActionResult> Index([FromQuery] ObservationQueryFilters filters)
    {
        var result = await _observationService.GetPagedObservationsAsync(filters);

        // Preparar dropdowns para los filtros
        ViewBag.AvailablePlants = await _plantService.GetPlantsForSelectionAsync();
        ViewBag.AvailableUsers = await _userService.GetUsersForSelectionAsync();
        ViewBag.CurrentFilters = filters; // Pasar los filtros actuales a la vista

        return View(result);
    }

    // GET: Admin/Observation/Create
    public async Task<IActionResult> Create()
    {
        var model = new ObservationCreateDto
        {
            AvailablePlants = await _plantService.GetPlantsForSelectionAsync()
        };
        return View(model);
    }

    // POST: Admin/Observation/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ObservationCreateDto model)
    {
        if (!ModelState.IsValid)
        {
            // Si la validación falla, volvemos a cargar las plantas para el dropdown
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