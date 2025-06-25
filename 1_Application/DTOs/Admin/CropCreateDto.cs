using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class CropCreateDto : ICropFormData // Implementar la interfaz
{
    // Las propiedades ya coinciden con la interfaz
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string CityName { get; set; } = string.Empty;
}