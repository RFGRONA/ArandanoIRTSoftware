using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Plants;

/// <summary>
///     DTO para mostrar una vista resumida de una planta, ideal para listas y tablas.
/// </summary>
public class PlantSummaryDto
{
    public int Id { get; set; }

    [Display(Name = "Nombre Planta")] public string Name { get; set; } = string.Empty;

    [Display(Name = "Cultivo")] public string CropName { get; set; } = string.Empty;

    [Display(Name = "Estado")] public string StatusName { get; set; } = string.Empty;

    [Display(Name = "Fecha de Registro")] public DateTime RegisteredAt { get; set; }
}