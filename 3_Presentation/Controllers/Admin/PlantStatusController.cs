using System.Security.Claims;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Plants;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

/// <summary>
/// Controlador para la gestión y visualización del estado de las plantas.
/// </summary>
[Area("Admin")]
[Authorize]
public class PlantStatusController : Controller
{
    private readonly IPlantService _plantService;
    private readonly IUserService _userService;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="PlantStatusController"/>.
    /// </summary>
    public PlantStatusController(IPlantService plantService, IUserService userService)
    {
        _plantService = plantService;
        _userService = userService;
    }

    // GET: /Admin/PlantStatus/Change/5
    // Muestra el formulario para cambiar el estado de una planta específica.
    /// <summary>
    /// Muestra el formulario para cambiar manualmente el estado de una planta específica.
    /// </summary>
    /// <param name="id">El ID de la planta cuyo estado se va a cambiar.</param>
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

    /// <summary>
    /// Procesa la solicitud para cambiar el estado de una planta.
    /// </summary>
    /// <param name="model">El modelo con los datos del nuevo estado y la observación.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Change(PlantStatusUpdateDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            ModelState.AddModelError("",
                "No se pudo identificar al usuario. Por favor, asegúrese de haber iniciado sesión.");
            return View(model);
        }

        if (!int.TryParse(userIdString, out var userId) || userId == 0)
        {
            ModelState.AddModelError("", "El identificador del usuario obtenido no es válido.");
            return View(model);
        }

        var result =
            await _plantService.UpdatePlantStatusAsync(model.PlantId, model.NewStatus, model.Observation, userId);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "El estado de la planta ha sido actualizado exitosamente.";
            return RedirectToAction("Details", "Plants", new { id = model.PlantId });
        }

        ModelState.AddModelError("", result.ErrorMessage);
        return View(model);
    }

    // GET: /Admin/PlantStatus/History
    public async Task<IActionResult> History(int? plantId, int? userId, DateTime? startDate, DateTime? endDate, string sortOrder = "desc")
    {
        if (!startDate.HasValue || !endDate.HasValue)
        {
            endDate = DateTime.Now;
            startDate = endDate.Value.AddDays(-7);
        }

        if (startDate > endDate) (startDate, endDate) = (endDate, startDate);

        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        ViewBag.SortOrder = sortOrder;

        var utcStartDate = startDate?.ToSafeUniversalTime();
        var utcEndDate = endDate?.Date.AddDays(1).AddTicks(-1).ToSafeUniversalTime();

        var historyList = await _plantService.GetPlantStatusHistoryAsync(plantId, userId, utcStartDate, utcEndDate);

        var history = sortOrder == "asc"
            ? historyList.OrderBy(h => h.ChangedAt)
            : historyList.OrderByDescending(h => h.ChangedAt);

        ViewBag.Plants = await _plantService.GetPlantsForSelectionAsync();
        ViewBag.Users = await _userService.GetUsersForSelectionAsync();

        return View(history);
    }
}