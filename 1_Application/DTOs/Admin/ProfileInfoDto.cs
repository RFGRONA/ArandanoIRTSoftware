using System.ComponentModel.DataAnnotations;
using ArandanoIRT.Web._0_Domain.Entities;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

/// <summary>
/// DTO para mostrar y actualizar la información del perfil de un usuario.
/// </summary>
public class ProfileInfoDto
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    [Display(Name = "Nombre")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido.")]
    [Display(Name = "Apellido")]
    public string LastName { get; set; } = string.Empty;

    public AccountSettings AccountSettings { get; set; } = new();
}