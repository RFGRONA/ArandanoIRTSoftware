using ArandanoIRT.Web._0_Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web._1_Application.DTOs.Plants;

/// <summary>
///     DTO para el formulario de creación de una nueva planta.
/// </summary>
public class PlantCreateDto : IPlantFormData
{
    public IEnumerable<SelectListItem> AvailableCrops { get; set; } = new List<SelectListItem>();
    public PlantStatus? Status { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CropId { get; set; }
    public ExperimentalGroupType? ExperimentalGroup { get; set; }
    public IEnumerable<SelectListItem> AvailableExperimentalGroups { get; set; } = new List<SelectListItem>();
}