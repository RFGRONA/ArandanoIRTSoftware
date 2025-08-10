using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class AdminLoginDto
{
    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    [Display(Name = "Nombre de Usuario")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; } // Para redirigir después del login
}