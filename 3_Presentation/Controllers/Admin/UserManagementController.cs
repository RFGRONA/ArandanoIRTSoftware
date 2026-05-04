using System.Security.Claims;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

/// <summary>
/// Controlador para que los administradores gestionen las cuentas de otros usuarios.
/// Incluye acciones como promover a administrador y eliminar cuentas de usuario,
/// con un flujo de trabajo seguro para la eliminación de administradores.
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserManagementController : BaseAdminController
{
    private readonly IUserService _userService;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="UserManagementController"/>.
    /// </summary>
    public UserManagementController(IUserService userService)
    {
        _userService = userService;
    }

    // GET: /Admin/UserManagement/Index
    public async Task<IActionResult> Index([FromQuery] UserQueryFilters filters)
    {
        var result = await _userService.GetPagedUsersForManagementAsync(filters);
        if (result.IsFailure)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage;
            return View(new ArandanoIRT.Web._1_Application.DTOs.Common.PagedResultDto<UserDto>());
        }

        ViewBag.CurrentFilters = filters;
        return View(result.Value);
    }

    // POST: /Admin/UserManagement/PromoteToAdmin/5
    /// <summary>
    /// Procesa la promoción de un usuario estándar al rol de Administrador.
    /// </summary>
    /// <param name="id">El ID del usuario a promover.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToAdmin(int id)
    {
        var result = await _userService.PromoteToAdminAsync(id);
        // Usamos una sobrecarga de HandleServiceResult que no requiere un modelo de vista en caso de fallo
        return HandleServiceResult(result, nameof(Index), nameof(Index));
    }

    // GET: /Admin/UserManagement/Delete/5
    /// <summary>
    /// Muestra una vista de confirmación antes de eliminar a un usuario estándar.
    /// </summary>
    /// <param name="id">El ID del usuario a eliminar.</param>
    public async Task<IActionResult> Delete(int id)
    {
        // Para la vista de confirmación, consultamos todos los usuarios (idealmente se debería tener un método GetUserForManagementById)
        var filter = new UserQueryFilters { PageSize = int.MaxValue };
        var usersResult = await _userService.GetPagedUsersForManagementAsync(filter);
        if (usersResult.IsFailure)
        {
            TempData[ErrorMessageKey] = usersResult.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        var userToDelete = usersResult.Value.Items.FirstOrDefault(u => u.Id == id);
        if (userToDelete == null)
        {
            TempData[ErrorMessageKey] = "Usuario no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        return View(userToDelete);
    }

    // POST: /Admin/UserManagement/Delete/5
    /// <summary>
    /// Ejecuta la eliminación de un usuario estándar tras la confirmación.
    /// </summary>
    /// <param name="id">El ID del usuario a eliminar.</param>
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var result = await _userService.DeleteUserAsync(id, currentUserId);
        return HandleServiceResult(result, nameof(Index), nameof(Index));
    }

    /// <summary>
    /// Inicia el proceso seguro para eliminar una cuenta de administrador, que requiere una segunda firma.
    /// </summary>
    /// <param name="model">El modelo que contiene el ID del administrador a eliminar y la contraseña del administrador actual para confirmación.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAdmin(AdminActionConfirmationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData[ErrorMessageKey] = "La contraseña es obligatoria para confirmar la eliminación.";
            return RedirectToAction(nameof(Index));
        }

        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Llamamos al nuevo método para iniciar la eliminación
        var result = await _userService.InitiateAdminDeletionAsync(
            model.AdminToDeleteId,
            currentUserId,
            model.CurrentAdminPassword,
            Url, // Pasamos el IUrlHelper
            Request.Scheme); // Pasamos el esquema (http/https)

        if (result.IsSuccess)
        {
            TempData[SuccessMessageKey] = $"Se ha iniciado el proceso para eliminar al administrador '{result.Value}'. Se ha enviado una solicitud de confirmación a los otros administradores.";
        }
        else
        {
            TempData[ErrorMessageKey] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index));
    }


    // GET: /UserManagement/ConfirmDeletion?id=X&token=Y
    /// <summary>
    /// Muestra la página de confirmación final para la eliminación de un administrador, accedida a través del enlace en el correo.
    /// </summary>
    /// <param name="id">El ID del administrador a eliminar.</param>
    /// <param name="token">El token de seguridad para la confirmación.</param>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmDeletion(int id, string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            TempData[ErrorMessageKey] = "El enlace de confirmación no es válido o ha expirado.";
            return RedirectToAction("Index", "Dashboard"); // Redirigimos al dashboard principal
        }

        var filter = new UserQueryFilters { PageSize = int.MaxValue };
        var usersResult = await _userService.GetPagedUsersForManagementAsync(filter);
        var userToDelete = usersResult.Value?.Items.FirstOrDefault(u => u.Id == id);

        if (userToDelete == null)
        {
            TempData[ErrorMessageKey] = "El usuario que se intenta eliminar ya no existe.";
            return RedirectToAction(nameof(Index));
        }

        // Pasamos el token a la vista a través de ViewBag para que el formulario lo pueda usar
        ViewBag.Token = token;
        return View(userToDelete); // Mostramos la vista de confirmación
    }

    /// <summary>
    /// Procesa la confirmación final y ejecuta la eliminación de la cuenta de administrador.
    /// </summary>
    /// <param name="id">El ID del administrador a eliminar.</param>
    /// <param name="token">El token de seguridad para la confirmación.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    [ActionName("ConfirmDeletion")]
    public async Task<IActionResult> ConfirmDeletionPost(int id, string token)
    {
        // Verificamos que el usuario que confirma esté logueado como Admin, como una capa extra de seguridad.
        if (!User.IsInRole("Admin"))
        {
            // Si un usuario no-admin intenta acceder, lo enviamos al login.
            return RedirectToAction("Login", "Account");
        }

        var result = await _userService.ConfirmAdminDeletionAsync(id, token);

        if (result.IsSuccess)
        {
            // En lugar de TempData, mostramos una vista final de éxito.
            return View("DeletionCompleted");
        }

        // Si la confirmación falla (ej. token expirado), lo mostramos en TempData y redirigimos.
        TempData[ErrorMessageKey] = result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }
}