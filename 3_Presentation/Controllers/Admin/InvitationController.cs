using System.Security.Claims;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class InvitationController : Controller
{
    private readonly IInvitationService _invitationService;

    public InvitationController(IInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    // Muestra la página con los botones para generar códigos
    public IActionResult Index()
    {
        return View();
    }

    // Acción para generar un código. Se llama desde el botón en la vista.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(string email, bool isAdmin)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
        {
            TempData["ErrorMessage"] = "El correo electrónico es obligatorio y debe ser válido.";
            return RedirectToAction(nameof(Index));
        }

        // Lógica mejorada para obtener un userId que puede ser nulo
        int? createdByUserId = null;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdString, out var parsedId))
        {
            createdByUserId = parsedId;
        }
        // Si el usuario es el ROOT, userIdString será nulo y createdByUserId se quedará como null. ¡Perfecto!

        var result = await _invitationService.CreateInvitationAsync(email, isAdmin, createdByUserId);

        if (result.IsSuccess)
        {
            var roleType = isAdmin ? "Administrador" : "Usuario";
            TempData["SuccessMessage"] = $"Invitación para '{roleType}' enviada exitosamente a {email}.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index));
    }
}