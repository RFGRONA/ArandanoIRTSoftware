using System.ComponentModel.DataAnnotations;
using ArandanoIRT.Web._1_Application.DTOs.Admin;

namespace ArandanoIRT.Web._1_Application.DTOs.Crops;

public class CropEditDto : ICropFormData // Implementar la interfaz
{
    [Required]
    public int Id { get; set; }

    // Las propiedades ya coinciden con la interfaz
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string CityName { get; set; } = string.Empty;
}