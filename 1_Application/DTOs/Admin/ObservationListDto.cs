using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class ObservationListDto
{
    public int Id { get; set; }

    [Display(Name = "Planta")] public string PlantName { get; set; } = string.Empty;

    [Display(Name = "Usuario")] public string UserName { get; set; } = string.Empty;

    [Display(Name = "Descripción")] public string Description { get; set; } = string.Empty;

    [Display(Name = "Calificación")] public short? SubjectiveRating { get; set; }

    [Display(Name = "Fecha de Creación")] public DateTime CreatedAt { get; set; }
}