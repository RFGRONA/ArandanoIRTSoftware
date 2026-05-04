using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

/// <summary>
/// DTO para representar la información de un usuario en listas y vistas de administración.
/// </summary>
public class UserDto
{
    public int Id { get; set; }

    [Display(Name = "Nombre Completo")] public string FullName { get; set; }

    [Display(Name = "Correo Electrónico")] public string Email { get; set; }

    [Display(Name = "Rol")] public string Role { get; set; }

    [Display(Name = "Fecha de Registro")] public DateTime RegisteredDate { get; set; }

    public bool IsDeletableByInactivity { get; set; } = false;
}