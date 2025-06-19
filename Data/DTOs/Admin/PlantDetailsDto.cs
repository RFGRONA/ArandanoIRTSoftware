using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web.Data.DTOs.Admin;

public class PlantDetailsDto
{
    public int Id { get; set; }
    [Display(Name = "Nombre Planta")]
    public string Name { get; set; } = string.Empty;
    [Display(Name = "Cultivo")]
    public string CropName { get; set; } = string.Empty;
    [Display(Name = "Ciudad del Cultivo")]
    public string CropCityName { get; set; } = string.Empty;
    [Display(Name = "Estado")]
    public string StatusName { get; set; } = string.Empty;
    [Display(Name = "Fecha de Registro")]
    public DateTime RegisteredAt { get; set; }
    [Display(Name = "Última Actualización")]
    public DateTime UpdatedAt { get; set; }
    // Más adelante: List<DeviceSummaryDto> AssociatedDevices { get; set; }
}