using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize] // Solo usuarios autenticados pueden acceder
public class ManageController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly IUserService _userService;

    public ManageController(UserManager<User> userManager, IUserService userService)
    {
        _userManager = userManager;
        _userService = userService;
    }

    // GET: /Admin/Manage/Index
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound("Usuario no encontrado.");

        var model = new ManageProfileViewModel
        {
            ProfileInfo = new ProfileInfoDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                AccountSettings = user.AccountSettings ?? new AccountSettings()
            }
        };

        return View(model);
    }

    // POST: /Admin/Manage/UpdateProfile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(ManageProfileViewModel model)
    {
        var result = await _userService.UpdateProfileAsync(User, model.ProfileInfo);
        if (result.IsSuccess)
            TempData["SuccessMessage"] = "Perfil actualizado exitosamente.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/Manage/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ManageProfileViewModel model)
    {
        var result = await _userService.ChangePasswordAsync(User, model.ChangePassword);
        if (result.IsSuccess)
            TempData["SuccessMessage"] = "Contraseña cambiada exitosamente.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }
}