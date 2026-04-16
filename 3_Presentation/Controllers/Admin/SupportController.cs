using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
public class SupportController : Controller
{
    private readonly ISupportService _supportService;

    public SupportController(ISupportService supportService)
    {
        _supportService = supportService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult PublicHelp()
    {
        return View();
    }

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

    [HttpGet]
    [Authorize]
    public IActionResult Index()
    {
        return View("AuthenticatedHelp");
    }

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

    [HttpGet]
    [AllowAnonymous]
    public IActionResult HelpRequestConfirmation()
    {
        return View();
    }
}