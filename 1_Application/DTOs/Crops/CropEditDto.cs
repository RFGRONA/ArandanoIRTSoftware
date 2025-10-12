using System.ComponentModel.DataAnnotations;
using ArandanoIRT.Web._0_Domain.Entities;

namespace ArandanoIRT.Web._1_Application.DTOs.Crops;

/// <summary>
/// DTO utilizado para capturar los datos del formulario de edición de un cultivo existente.
/// </summary>
public class CropEditDto : ICropFormData
{
    [Required]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string CityName { get; set; } = string.Empty;
    public CropSettings CropSettings { get; set; }
}