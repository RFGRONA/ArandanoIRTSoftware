using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class RegisterDto
{
    [Required]
    [Display(Name = "Código de Invitación")]
    public string InvitationCode { get; set; } = string.Empty;

    [Required][EmailAddress] public string Email { get; set; } = string.Empty;

    [Required][Display(Name = "Nombre")] public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Apellido")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}