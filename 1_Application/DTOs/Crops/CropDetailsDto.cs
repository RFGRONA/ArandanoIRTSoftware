using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Crops;

public class CropDetailsDto
{
    public int Id { get; set; }
    [Display(Name = "Nombre del Cultivo")]
    public string Name { get; set; } = string.Empty;
    [Display(Name = "Dirección")]
    public string? Address { get; set; }
    [Display(Name = "Ciudad (para WeatherAPI)")]
    public string CityName { get; set; } = string.Empty;
    [Display(Name = "Fecha de Creación")]
    public DateTime CreatedAt { get; set; }
    [Display(Name = "Última Actualización")]
    public DateTime UpdatedAt { get; set; }
    // Más adelante podríamos añadir: List<PlantSummaryDto> AssociatedPlants { get; set; }
}