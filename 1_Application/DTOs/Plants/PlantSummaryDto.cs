using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Plants;

public class PlantSummaryDto
{
    public int Id { get; set; }
    [Display(Name = "Nombre Planta")]
    public string Name { get; set; } = string.Empty;
    [Display(Name = "Cultivo")]
    public string CropName { get; set; } = string.Empty; // Nombre del cultivo asociado
    [Display(Name = "Estado")]
    public string StatusName { get; set; } = string.Empty; // Nombre del estado
    [Display(Name = "Fecha de Registro")]
    public DateTime RegisteredAt { get; set; }
}