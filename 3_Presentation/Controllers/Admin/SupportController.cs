using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

/// <summary>
/// Controlador que gestiona las solicitudes de soporte técnico.
/// Ofrece flujos separados para usuarios públicos (no autenticados) y usuarios autenticados.
/// </summary>
[Area("Admin")]
public class SupportController : Controller
{
    private readonly ISupportService _supportService;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="SupportController"/>.
    /// </summary>
    public SupportController(ISupportService supportService)
    {
        _supportService = supportService;
    }

    /// <summary>
    /// Muestra el formulario de ayuda público para usuarios no autenticados.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult PublicHelp()
    {
        return View();
    }

    /// <summary>
    /// Procesa el envío del formulario de ayuda público.
    /// </summary>
    /// <param name="model">Los datos de la solicitud de ayuda.</param>
    /// <returns>Una vista de confirmación genérica, independientemente del resultado, por seguridad.</returns>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ValidateTurnstile]
    public async Task<IActionResult> PublicHelp(PublicHelpRequestDto model)
    {
        if (!ModelState.IsValid) return View(model);
        await _supportService.ProcessPublicHelpRequestAsync(model);
        return View("HelpRequestConfirmation");
    }

    /// <summary>
    /// Muestra el formulario de ayuda para usuarios que ya han iniciado sesión.
    /// </summary>
    [HttpGet]
    [Authorize]
    public IActionResult Index()
    {
        return View("AuthenticatedHelp");
    }

    /// <summary>
    /// Procesa el envío del formulario de ayuda de un usuario autenticado.
    /// </summary>
    /// <param name="model">Los datos de la solicitud de ayuda.</param>
    /// <returns>Una redirección al dashboard con un mensaje de éxito si el envío es correcto.</returns>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AuthenticatedHelp(AuthenticatedHelpRequestDto model)
    {
        if (!ModelState.IsValid) return View("AuthenticatedHelp", model);

        await _supportService.ProcessAuthenticatedHelpRequestAsync(model, User);

        TempData["SuccessMessage"] = "Tu solicitud de ayuda ha sido enviada. Pronto nos pondremos en contacto contigo.";
        return RedirectToAction("Index", "Dashboard");
    }

    /// <summary>
    /// Muestra la página de confirmación genérica después de enviar una solicitud de ayuda pública.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult HelpRequestConfirmation()
    {
        return View();
    }
}