using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Plants;

public class PlantStatusHistoryDto
{
    public long Id { get; set; }

    [Display(Name = "Planta")] public string PlantName { get; set; } = string.Empty;

    [Display(Name = "Estado Asignado")] public string Status { get; set; } = string.Empty;

    [Display(Name = "Observación")] public string? Observation { get; set; }

    [Display(Name = "Fuente del Cambio")] public string Source { get; set; } = string.Empty;

    [Display(Name = "Fecha del Cambio")] public DateTime ChangedAt { get; set; }
}