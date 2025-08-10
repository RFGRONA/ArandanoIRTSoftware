using System.Security.Claims;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserManagementController : BaseAdminController
{
    private readonly IUserService _userService;

    public UserManagementController(IUserService userService)
    {
        _userService = userService;
    }

    // GET: /Admin/UserManagement/Index
    public async Task<IActionResult> Index()
    {
        var result = await _userService.GetAllUsersForManagementAsync();
        if (result.IsFailure)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage;
            return View(new List<UserDto>());
        }

        return View(result.Value);
    }

    // POST: /Admin/UserManagement/PromoteToAdmin/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToAdmin(int id)
    {
        var result = await _userService.PromoteToAdminAsync(id);
        // Usamos una sobrecarga de HandleServiceResult que no requiere un modelo de vista en caso de fallo
        return HandleServiceResult(result, nameof(Index), nameof(Index));
    }

    // GET: /Admin/UserManagement/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        // Para la vista de confirmación, podemos reutilizar el UserDto
        // obteniendo los datos de todos los usuarios y filtrando por el ID.
        var usersResult = await _userService.GetAllUsersForManagementAsync();
        if (usersResult.IsFailure)
        {
            TempData[ErrorMessageKey] = usersResult.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        var userToDelete = usersResult.Value.FirstOrDefault(u => u.Id == id);
        if (userToDelete == null)
        {
            TempData[ErrorMessageKey] = "Usuario no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        return View(userToDelete);
    }

    // POST: /Admin/UserManagement/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var result = await _userService.DeleteUserAsync(id, currentUserId);
        return HandleServiceResult(result, nameof(Index), nameof(Index));
    }

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
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmDeletion(int id, string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            TempData[ErrorMessageKey] = "El enlace de confirmación no es válido o ha expirado.";
            return RedirectToAction("Index", "Dashboard"); // Redirigimos al dashboard principal
        }

        var usersResult = await _userService.GetAllUsersForManagementAsync();
        var userToDelete = usersResult.Value?.FirstOrDefault(u => u.Id == id);

        if (userToDelete == null)
        {
            TempData[ErrorMessageKey] = "El usuario que se intenta eliminar ya no existe.";
            return RedirectToAction(nameof(Index));
        }

        // Pasamos el token a la vista a través de ViewBag para que el formulario lo pueda usar
        ViewBag.Token = token;
        return View(userToDelete); // Mostramos la vista de confirmación
    }

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