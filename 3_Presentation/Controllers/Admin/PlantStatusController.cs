using System.Security.Claims;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Plants;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize]
public class PlantStatusController : Controller
{
    private readonly IPlantService _plantService;
    private readonly IUserService _userService;

    public PlantStatusController(IPlantService plantService, IUserService userService)
    {
        _plantService = plantService;
        _userService = userService;
    }

    // GET: /Admin/PlantStatus/Change/5
    // Muestra el formulario para cambiar el estado de una planta específica.
    [HttpGet]
    public async Task<IActionResult> Change(int id)
    {
        var plantResult = await _plantService.GetPlantByIdAsync(id);
        if (!plantResult.IsSuccess || plantResult.Value == null) return NotFound();

        var model = new PlantStatusUpdateDto
        {
            PlantId = plantResult.Value.Id,
            PlantName = plantResult.Value.Name,
            NewStatus = plantResult.Value.Status
        };
        ViewBag.AvailableStatuses = EnumSelectListExtensions.ToSelectList<PlantStatus>();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // RECOMENDACIÓN: Activa la autorización para asegurar que solo usuarios logueados lleguen aquí.
    // [Authorize(Roles = "Admin")] 
    public async Task<IActionResult> Change(PlantStatusUpdateDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            ModelState.AddModelError("", "No se pudo identificar al usuario. Por favor, asegúrese de haber iniciado sesión.");
            return View(model);
        }

        if (!int.TryParse(userIdString, out var userId) || userId == 0)
        {
            ModelState.AddModelError("", "El identificador del usuario obtenido no es válido.");
            return View(model);
        }

        var result = await _plantService.UpdatePlantStatusAsync(model.PlantId, model.NewStatus, model.Observation, userId);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "El estado de la planta ha sido actualizado exitosamente.";
            return RedirectToAction("Details", "Plants", new { id = model.PlantId });
        }

        ModelState.AddModelError("", result.ErrorMessage);
        return View(model);
    }

    // GET: /Admin/PlantStatus/History
    public async Task<IActionResult> History(int? plantId, int? userId, DateTime? startDate, DateTime? endDate)
    {
        var utcStartDate = startDate?.ToSafeUniversalTime();
        var utcEndDate = endDate?.Date.AddDays(1).AddTicks(-1).ToSafeUniversalTime();
        
        var history = await _plantService.GetPlantStatusHistoryAsync(plantId, userId, utcStartDate, utcEndDate);

        ViewBag.Plants = await _plantService.GetPlantsForSelectionAsync();
        ViewBag.Users = await _userService.GetUsersForSelectionAsync();

        return View(history);
    }
}