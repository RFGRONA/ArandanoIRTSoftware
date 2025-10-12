using ArandanoIRT.Web._0_Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Crops;

/// <summary>
/// DTO utilizado para capturar los datos del formulario de creación de un nuevo cultivo.
/// </summary>
public class CropCreateDto : ICropFormData
{
    [Required(ErrorMessage = "El nombre del cultivo es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    [Required(ErrorMessage = "El nombre de la ciudad es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre de la ciudad no puede tener más de 100 caracteres.")]
    public string CityName { get; set; } = string.Empty;

    public CropSettings CropSettings { get; set; } = new();
}