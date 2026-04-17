using ArandanoIRT.Web._1_Application.DTOs.Admin;

namespace ArandanoIRT.Web._3_Presentation.ViewModels.Admin;

/// <summary>
///     ViewModel para la página de "Gestionar Perfil".
///     Agrupa los DTOs necesarios para los formularios de edición de perfil y cambio de contraseña.
/// </summary>
public class ManageProfileViewModel
{
    /// <summary>
    ///     Datos para el formulario de información del perfil.
    /// </summary>
    public ProfileInfoDto ProfileInfo { get; set; } = new();

    /// <summary>
    ///     Datos para el formulario de cambio de contraseña.
    /// </summary>
    public ChangePasswordDto ChangePassword { get; set; } = new();
}