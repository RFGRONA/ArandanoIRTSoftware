using System.Security.Claims;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")] // Proteger todo el controlador para que solo los administradores puedan acceder
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
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var result = await _userService.DeleteUserAsync(id, currentUserId);
        return HandleServiceResult(result, nameof(Index), nameof(Index));
    }
}