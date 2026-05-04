using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Crops;

/// <summary>
/// DTO para mostrar una vista resumida de un cultivo, típicamente en una lista o tabla.
/// </summary>
public class CropSummaryDto
{
    public int Id { get; set; }
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;
    [Display(Name = "Ciudad")]
    public string CityName { get; set; } = string.Empty;
    [Display(Name = "Fecha de Creación")]
    public DateTime CreatedAt { get; set; }
}