using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web._1_Application.DTOs.Observations;

public class ObservationCreateDto
{
    [Required(ErrorMessage = "Debe seleccionar una planta.")]
    [Display(Name = "Planta Asociada")]
    public int PlantId { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, ErrorMessage = "La descripción no puede exceder los 2000 caracteres.")]
    [Display(Name = "Descripción de la Observación")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5.")]
    [Display(Name = "Calificación Subjetiva (1-3, Opcional)")]
    public short? SubjectiveRating { get; set; }

    // Propiedad para poblar el dropdown de plantas en la vista
    public IEnumerable<SelectListItem> AvailablePlants { get; set; } = new List<SelectListItem>();
}