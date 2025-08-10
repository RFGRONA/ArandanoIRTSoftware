using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ArandanoIRT.Web._0_Domain.Entities;

namespace ArandanoIRT.Web._3_Presentation.ViewComponents
{
    public class UserInfoViewComponent : ViewComponent
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public UserInfoViewComponent(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (UserClaimsPrincipal.IsInRole("BootstrapAdmin"))
            {
                return View("Default", (Name: "ROOT_BOOTSTRAP_USER", Role: "BootstrapAdmin"));
            }

            if (!_signInManager.IsSignedIn(UserClaimsPrincipal))
            {
                return Content(string.Empty);
            }

            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            if (user == null)
            {
                return Content("Usuario no encontrado.");
            }

            var userRole = User.IsInRole("Admin") ? "Administrador" : "Usuario";
            var model = (Name: $"{user.FirstName} {user.LastName}", Role: userRole);

            return View(model);
        }
    }
}