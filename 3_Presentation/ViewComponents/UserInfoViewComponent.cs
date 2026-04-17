using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ArandanoIRT.Web._0_Domain.Entities;

namespace ArandanoIRT.Web._3_Presentation.ViewComponents
{
/// <summary>
///     Un ViewComponent que se encarga de obtener y mostrar la información del usuario actualmente autenticado (nombre y
///     rol).
///     Generalmente se utiliza en el layout principal de la aplicación.
/// </summary>
    public class UserInfoViewComponent : ViewComponent
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="UserInfoViewComponent" />.
    /// </summary>
        public UserInfoViewComponent(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

    /// <summary>
    ///     Método invocado cuando se renderiza el componente.
    ///     Obtiene los datos del usuario logueado y los pasa a la vista parcial correspondiente.
    /// </summary>
    /// <returns>La vista del componente con los datos del usuario.</returns>
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