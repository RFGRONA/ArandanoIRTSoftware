using ArandanoIRT.Web._1_Application.DTOs.Admin;

namespace ArandanoIRT.Web._3_Presentation.ViewModels.Admin;

/// <summary>
///     ViewModel para la página de gestión de usuarios.
///     Contiene la lista de usuarios a mostrar en la tabla de administración.
/// </summary>
public class UserManagementViewModel
{
    public List<UserDto> Users { get; set; } = new();
}