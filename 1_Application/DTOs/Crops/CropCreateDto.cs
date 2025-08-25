using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using System.ComponentModel.DataAnnotations; // Necesario para las validaciones

namespace ArandanoIRT.Web._1_Application.DTOs.Crops;

public class CropCreateDto : ICropFormData
{
    [Required(ErrorMessage = "El nombre del cultivo es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(200, ErrorMessage = "La dirección no puede tener más de 200 caracteres.")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "El nombre de la ciudad es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre de la ciudad no puede tener más de 100 caracteres.")]
    public string CityName { get; set; } = string.Empty;

    public CropSettings CropSettings { get; set; } = new();
}
