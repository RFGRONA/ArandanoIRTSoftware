using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

/// <summary>
/// DTO para manejar las solicitudes de ayuda enviadas por usuarios que ya están autenticados en el sistema.
/// </summary>
public class AuthenticatedHelpRequestDto
{
    [Required(ErrorMessage = "El asunto es requerido.")]
    [StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "El mensaje es requerido.")]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;
}