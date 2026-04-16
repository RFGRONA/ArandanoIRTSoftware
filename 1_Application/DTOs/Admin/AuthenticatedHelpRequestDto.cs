using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class AuthenticatedHelpRequestDto
{
    [Required(ErrorMessage = "El asunto es requerido.")]
    [StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "El mensaje es requerido.")]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;
}