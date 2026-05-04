using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

/// <summary>
/// DTO para el formulario de solicitud de restablecimiento de contraseña.
/// </summary>
public class ForgotPasswordDto
{
    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [Display(Name = "Correo Electrónico de tu Cuenta")]
    public string Email { get; set; } = string.Empty;
}