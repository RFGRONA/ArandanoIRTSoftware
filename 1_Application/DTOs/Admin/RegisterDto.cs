using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class RegisterDto
{
    [Required(ErrorMessage = "El campo \"{0}\" es obligatorio.")]
    [Display(Name = "Código de Invitación")]
    public string InvitationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo \"{0}\" es obligatorio.")]
    [EmailAddress(ErrorMessage = "El campo \"{0}\" no es una dirección de correo electrónico válida.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo \"{0}\" es obligatorio.")]
    [Display(Name = "Nombre")] 
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo \"{0}\" es obligatorio.")]
    [Display(Name = "Apellido")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo \"{0}\" es obligatorio.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo \"{0}\" es obligatorio.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
