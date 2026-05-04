using System.Text;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

/// <summary>
///     Controlador que maneja todas las acciones relacionadas con la cuenta de usuario,
///     como inicio de sesión, cierre de sesión, registro y recuperación de contraseña.
/// </summary>
[Area("Admin")]
public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly SignInManager<User> _signInManager;
    private readonly IUserService _userService;
    private readonly IAlertService _alertService;
    private readonly UserManager<User> _userManager;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="AccountController" />.
    /// </summary>
    public AccountController(
        SignInManager<User> signInManager,
        IUserService userService,
        IAlertService alertService,
        UserManager<User> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userService = userService;
        _alertService = alertService;
        _userManager = userManager;
        _logger = logger;
    }

    // --- LOGIN ---
    /// <summary>
    ///     Muestra la página de inicio de sesión.
    /// </summary>
    /// <param name="returnUrl">La URL a la que se redirigirá al usuario después de un inicio de sesión exitoso.</param>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Dashboard");
        // Pasamos el nuevo DTO a la vista
        var model = new LoginDto { ReturnUrl = returnUrl };
        return View(model);
    }

    /// <summary>
    ///     Procesa la solicitud de inicio de sesión de un usuario.
    /// </summary>
    /// <param name="model">Los datos del formulario de inicio de sesión.</param>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ValidateTurnstile]
    public async Task<IActionResult> Login(LoginDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var (result, justLockedOut) = await _userService.LoginUserAsync(model);

        if (result.Succeeded)
        {
            _logger.LogInformation("Usuario {Email} inició sesión.", model.Email);
            return RedirectToLocal(model.ReturnUrl);
        }

        // Si la cuenta se acaba de bloquear en este intento, generamos el enlace y enviamos la alerta.
        if (justLockedOut)
        {
            _logger.LogWarning("Cuenta de usuario {Email} bloqueada. Generando alerta con enlace de reseteo.", model.Email);
            ModelState.AddModelError("", "Esta cuenta ha sido bloqueada por demasiados intentos fallidos. Por favor, revise su correo electrónico.");

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var forgotPasswordUrl = Url.Action("ForgotPassword", "Account", null, Request.Scheme);
                if (forgotPasswordUrl != null)
                {
                    await _alertService.TriggerFailedLoginAlertAsync(user, forgotPasswordUrl);
                }
            }
        }
        else if (result.IsLockedOut)
        {
            _logger.LogWarning("Intento de login en cuenta ya bloqueada: {Email}", model.Email);
            ModelState.AddModelError("", "Esta cuenta ya se encuentra bloqueada, por favor inténtelo más tarde.");
        }
        else
        {
            ModelState.AddModelError("", "Intento de inicio de sesión inválido.");
        }

        return View(model);
    }

    // --- LOGOUT ---
    /// <summary>
    ///     Cierra la sesión del usuario actual.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Usuario cerró sesión.");
        return RedirectToAction(nameof(Login));
    }

    // --- REGISTRO ---
    /// <summary>
    ///     Muestra la página de registro de un nuevo usuario.
    /// </summary>
    /// <param name="code">El código de invitación (opcionalmente pasado por la URL).</param>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? code)
    {
        var model = new RegisterDto { InvitationCode = code ?? string.Empty };
        return View(model);
    }

    /// <summary>
    ///     Procesa la solicitud de registro de un nuevo usuario.
    /// </summary>
    /// <param name="model">Los datos del formulario de registro.</param>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ValidateTurnstile]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.RegisterUserAsync(model);

        if (result.IsSuccess) return RedirectToAction("Index", "Dashboard");

        ModelState.AddModelError("", result.ErrorMessage);
        return View(model);
    }

    // --- ACCESO DENEGADO ---
    /// <summary>
    ///     Muestra la página de "Acceso Denegado".
    /// </summary>
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    ///     Redirige a una URL local de forma segura para prevenir ataques de redirección abierta.
    ///     Si la URL no es local, redirige a la página principal del dashboard.
    /// </summary>
    /// <param name="returnUrl">La URL de destino.</param>
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard");
    }

    // --- RECUPERACIÓN DE CONTRASEÑA ---

    // GET: /Admin/Account/ForgotPassword
    /// <summary>
    ///     Muestra el formulario para solicitar el restablecimiento de contraseña.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    // POST: /Admin/Account/ForgotPassword
    /// <summary>
    ///     Procesa la solicitud de restablecimiento de contraseña.
    /// </summary>
    /// <param name="model">El DTO con el correo electrónico del usuario.</param>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ValidateTurnstile]
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
    /// <summary>
    ///     Muestra el formulario para establecer una nueva contraseña.
    /// </summary>
    /// <param name="token">El token de restablecimiento.</param>
    /// <param name="email">El correo del usuario.</param>
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
    /// <summary>
    ///     Procesa el envío del formulario con la nueva contraseña.
    /// </summary>
    /// <param name="model">El DTO con el token, correo y la nueva contraseña.</param>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ValidateTurnstile]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.ResetPasswordAsync(model);

        if (result.IsSuccess) return View("ResetPasswordConfirmation");

        ModelState.AddModelError("", result.ErrorMessage);
        return View(model);
    }
}