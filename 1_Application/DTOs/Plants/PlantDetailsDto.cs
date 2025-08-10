using System.ComponentModel.DataAnnotations;
using ArandanoIRT.Web._0_Domain.Enums;

namespace ArandanoIRT.Web._1_Application.DTOs.Plants;

public class PlantDetailsDto
{
    public int Id { get; set; }

    [Display(Name = "Nombre Planta")] public string Name { get; set; } = string.Empty;

    [Display(Name = "Cultivo")] public string CropName { get; set; } = string.Empty;

    [Display(Name = "Ciudad del Cultivo")] public string CropCityName { get; set; } = string.Empty;

    [Display(Name = "Estado")] public PlantStatus Status { get; set; }

    [Display(Name = "Grupo Experimental")] public ExperimentalGroupType ExperimentalGroup { get; set; }

    [Display(Name = "Fecha de Registro")] public DateTime RegisteredAt { get; set; }

    [Display(Name = "Última Actualización")]
    public DateTime UpdatedAt { get; set; }

    public string? ThermalMaskData { get; set; }
}