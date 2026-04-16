using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "La contraseña actual es requerida.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña Actual")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es requerida.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva Contraseña")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Nueva Contraseña")]
    [Compare("NewPassword", ErrorMessage = "La nueva contraseña y la confirmación no coinciden.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}