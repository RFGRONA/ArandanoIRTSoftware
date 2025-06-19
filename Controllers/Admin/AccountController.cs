using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
// Para AdminCredentialsSettings
using ArandanoIRT.Web.Configuration;
using ArandanoIRT.Web.Data.DTOs.Admin; // Para AdminLoginDto

namespace ArandanoIRT.Web.Controllers.Admin;

[Area("Admin")] // Especifica el área para las rutas
public class AccountController : Controller
{
    private readonly AdminCredentialsSettings _adminCredentials;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IOptions<AdminCredentialsSettings> adminCredentialsOptions, ILogger<AccountController> logger)
    {
        _adminCredentials = adminCredentialsOptions.Value;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous] // Permite acceso anónimo a la página de login
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        // Si el usuario ya está autenticado, redirigirlo (ej. al dashboard)
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            _logger.LogInformation("Usuario ya autenticado, redirigiendo a Admin/Dashboard.");
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
        return View(new AdminLoginDto { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginDto model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Verificar credenciales
        // Aquí usamos la comparación de hash BCRYPT
        bool isValidUser = model.Username == _adminCredentials.Username &&
                           BCrypt.Net.BCrypt.Verify(model.Password, _adminCredentials.PasswordHash);

        if (isValidUser)
        {
            _logger.LogInformation("Credenciales válidas para el usuario {Username}.", model.Username);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Username),
                new Claim(ClaimTypes.Role, "Admin") // Asignar el rol de Admin
                // Puedes añadir más claims si es necesario
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true, // Permite refrescar la cookie
                IsPersistent = true, // La cookie persiste entre sesiones del navegador (hasta que expire)
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) // Coincide con la configuración en Program.cs
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("Usuario {Username} autenticado exitosamente. Redirigiendo.", model.Username);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            else
            {
                // Redirigir al Dashboard del Admin por defecto
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
        }

        _logger.LogWarning("Intento de login fallido para el usuario {Username}.", model.Username);
        ModelState.AddModelError(string.Empty, "Nombre de usuario o contraseña incorrectos.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // [Authorize(Roles = "Admin")] // Solo usuarios autenticados como Admin pueden hacer logout
    [Authorize] // Alternativamente, cualquier usuario autenticado puede hacer logout
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("Usuario {Username} cerrando sesión.", User.Identity?.Name ?? "Desconocido");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account", new { area = "Admin" }); // O a la página principal del sitio
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        _logger.LogWarning("Acceso denegado solicitado.");
        return View(); // Crea una vista simple para AccessDenied
    }
}