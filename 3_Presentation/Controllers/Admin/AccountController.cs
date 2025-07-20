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
    private readonly IUserService _userService; // Ya lo teníamos
    private readonly IAlertService _alertService;

    public AccountController(
        SignInManager<User> signInManager,
        IUserService userService,
        IAlertService alertService,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userService = userService;
        _alertService = alertService;
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

    // --- RECUPERACIÓN DE CONTRASEÑA ---

// GET: /Admin/Account/ForgotPassword
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

// POST: /Admin/Account/ForgotPassword
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.GeneratePasswordResetAsync(model, Url, Request.Scheme);

        if (result.IsSuccess && !string.IsNullOrEmpty(result.Value.Name))
        {
            // AlertService se encarga de todo
            await _alertService.SendPasswordResetEmailAsync(model.Email, result.Value.Name, result.Value.ResetLink);
        }

        // Por seguridad, siempre mostramos el mismo mensaje, exista o no el correo.
        return View("ForgotPasswordConfirmation");
    }

// GET: /Admin/Account/ResetPassword
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? token = null, string? email = null)
    {
        if (token == null || email == null)
            ModelState.AddModelError("", "El enlace para restablecer la contraseña no es válido o ha expirado.");

        var model = new ResetPasswordDto { Token = token, Email = email };
        return View(model);
    }

// POST: /Admin/Account/ResetPassword
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.ResetPasswordAsync(model);

        if (result.IsSuccess) return View("ResetPasswordConfirmation");

        ModelState.AddModelError("", result.ErrorMessage);
        return View(model);
    }
}