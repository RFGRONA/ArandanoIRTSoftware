using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly SignInManager<User> _signInManager;
    private readonly IUserService _userService;

    public AccountController(
        IUserService userService,
        SignInManager<User> signInManager,
        ILogger<AccountController> logger)
    {
        _userService = userService;
        _signInManager = signInManager;
        _logger = logger;
    }

    // --- LOGIN ---
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Dashboard");
        // Pasamos el nuevo DTO a la vista
        var model = new LoginDto { ReturnUrl = returnUrl };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.LoginUserAsync(model); // El servicio ya usa el DTO correcto

        if (result.Succeeded)
        {
            _logger.LogInformation("Usuario {Email} inició sesión.", model.Email);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Cuenta de usuario {Email} bloqueada.", model.Email);
            ModelState.AddModelError("", "Esta cuenta ha sido bloqueada, por favor intente más tarde.");
        }
        else
        {
            ModelState.AddModelError("", "Intento de inicio de sesión inválido.");
        }

        return View(model);
    }

    // --- LOGOUT ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Usuario cerró sesión.");
        return RedirectToAction(nameof(Login));
    }

    // --- REGISTRO ---
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? code)
    {
        var model = new RegisterDto { InvitationCode = code ?? string.Empty };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.RegisterUserAsync(model);

        if (result.IsSuccess) return RedirectToAction("Index", "Dashboard");

        ModelState.AddModelError("", result.ErrorMessage);
        return View(model);
    }

    // --- ACCESO DENEGADO ---
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard");
    }
}