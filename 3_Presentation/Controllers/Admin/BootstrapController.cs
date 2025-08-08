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

[Area("Admin")]
public class BootstrapController : Controller
{
    private readonly RoleManager<ApplicationRole> _roleManager; // NUEVO
    private readonly AdminCredentialsSettings _rootCredentials;
    private readonly UserManager<User> _userManager; // NUEVO

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
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginDto model)
    {
        // VERIFICACIÓN: Doble chequeo en el POST por seguridad.
        if (await AdminUserExistsAsync()) return NotFound();

        if (!ModelState.IsValid) return View(model);

        var isValidRootUser = model.Username == _rootCredentials.Username &&
                              BCrypt.Net.BCrypt.Verify(model.Password, _rootCredentials.PasswordHash);

        if (isValidRootUser)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "ROOT_BOOTSTRAP_USER"),
                new(ClaimTypes.Role, "BootstrapAdmin")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
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