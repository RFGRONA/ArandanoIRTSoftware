using System.Security.Claims;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

/// <summary>
///     Controlador para la gestión de invitaciones de nuevos usuarios.
///     Permite a los administradores y al usuario de arranque especial ('BootstrapAdmin') enviar invitaciones por correo
///     electrónico.
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin,BootstrapAdmin")]
public class InvitationController : Controller
{
    private readonly IInvitationService _invitationService;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="InvitationController" />.
    /// </summary>
    public InvitationController(IInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    /// <summary>
    ///     Muestra la página principal desde donde se pueden generar las invitaciones.
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    ///     Procesa la solicitud para generar y enviar una nueva invitación por correo electrónico.
    /// </summary>
    /// <param name="email">El correo electrónico del destinatario de la invitación.</param>
    /// <param name="isAdmin">Indica si la invitación es para crear una cuenta de administrador.</param>
    /// <returns>Una redirección a la página principal de invitaciones con un mensaje de éxito o error.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(string email, bool isAdmin)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
        {
            TempData["ErrorMessage"] = "El correo electrónico es obligatorio y debe ser válido.";
            return RedirectToAction(nameof(Index));
        }

        int? createdByUserId = null;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdString, out var parsedId)) createdByUserId = parsedId;
        // Nota: Si el usuario es el 'BootstrapAdmin', no tendrá un ID numérico, por lo que `createdByUserId` permanecerá nulo, lo cual es correcto.

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