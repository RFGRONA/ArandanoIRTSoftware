using System.ComponentModel.DataAnnotations;
using ArandanoIRT.Web._0_Domain.Enums;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class PlantStatusUpdateDto
{
    [Required] public int PlantId { get; set; }

    public string PlantName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debes seleccionar un nuevo estado.")]
    public PlantStatus NewStatus { get; set; }

    [StringLength(500, ErrorMessage = "La observación no puede exceder los 500 caracteres.")]
    [Display(Name = "Observación (Opcional)")]
    public string? Observation { get; set; }
}