using System.Security.Claims;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

// NUEVO

/// <summary>
///     Controlador de propósito especial utilizado únicamente para la configuración inicial de la aplicación.
///     Permite un inicio de sesión temporal con credenciales pre-configuradas para crear el primer administrador.
///     Sus endpoints se desactivan (devuelven 404) tan pronto como existe al menos un usuario con el rol "Admin".
/// </summary>
[Area("Admin")]
public class BootstrapController : Controller
{
    private readonly RoleManager<ApplicationRole> _roleManager; // NUEVO
    private readonly AdminCredentialsSettings _rootCredentials;
    private readonly UserManager<User> _userManager; // NUEVO

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="BootstrapController" />.
    /// </summary>
    public BootstrapController(
        IOptions<AdminCredentialsSettings> adminCredentialsOptions,
        UserManager<User> userManager, // NUEVO
        RoleManager<ApplicationRole> roleManager) // NUEVO
    {
        _rootCredentials = adminCredentialsOptions.Value;
        _userManager = userManager; // NUEVO
        _roleManager = roleManager; // NUEVO
    }

    // GET: /Admin/Bootstrap/Login
    /// <summary>
    ///     Muestra la página de inicio de sesión de arranque, solo si no existen administradores en el sistema.
    /// </summary>
    /// <returns>La vista de login o un `NotFound (404)` si la configuración inicial ya se completó.</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login()
    {
        // VERIFICACIÓN: Si ya existe un admin, este endpoint se desactiva.
        if (await AdminUserExistsAsync())
            return NotFound(); // Devolvemos un 404 para que parezca que la página no existe.
        return View();
    }

    // POST: /Admin/Bootstrap/Login
    /// <summary>
    ///     Procesa la solicitud de inicio de sesión de arranque. Si las credenciales son válidas,
    ///     inicia una sesión temporal con un rol especial 'BootstrapAdmin' para permitir la creación de la primera invitación.
    /// </summary>
    /// <param name="model">Las credenciales de inicio de sesión de arranque.</param>
    /// <returns>Una redirección al controlador de invitaciones en caso de éxito, o la vista con un error en caso de fallo.</returns>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginDto model)
    {
        // VERIFICACIÓN: Doble chequeo en el POST por seguridad.
        if (await AdminUserExistsAsync()) return NotFound();

        if (!ModelState.IsValid) return View(model);

        if (string.IsNullOrWhiteSpace(_rootCredentials.PasswordHash))
        {
            ModelState.AddModelError("", "Credenciales de arranque no configuradas correctamente.");
            return View(model);
        }

        var isValidRootUser = model.Username == _rootCredentials.Username &&
                              BCrypt.Net.BCrypt.Verify(model.Password, _rootCredentials.PasswordHash);

        if (isValidRootUser)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "Bootstrap User"),
                new(ClaimTypes.Role, "BootstrapAdmin")
            };

            var claimsIdentity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Invitation");
        }

        ModelState.AddModelError(string.Empty, "Credenciales de arranque incorrectas.");
        return View(model);
    }

    /// <summary>
    ///     Verifica si ya existe al menos un usuario con el rol de "Admin" en la base de datos.
    /// </summary>
    private async Task<bool> AdminUserExistsAsync()
    {
        // Primero, comprobamos si el rol "Admin" siquiera existe.
        if (!await _roleManager.RoleExistsAsync("Admin")) return false; // Si no hay rol, no puede haber usuarios en él.

        // Si el rol existe, vemos si hay usuarios asignados a él.
        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        return adminUsers.Any(); // Devuelve true si la lista tiene al menos un usuario.
    }
}