using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class PublicHelpRequestDto
{
    [Required(ErrorMessage = "Tu nombre es requerido.")]
    [StringLength(100)]
    [Display(Name = "Tu Nombre")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tu correo es requerido.")]
    [StringLength(100)]
    [EmailAddress]
    [Display(Name = "Tu Correo de Contacto")]
    public string ContactEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "El asunto es requerido.")]
    [StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "El mensaje es requerido.")]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;
}