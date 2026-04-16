using System.ComponentModel.DataAnnotations;
using ArandanoIRT.Web._0_Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

// Para SelectListItem

namespace ArandanoIRT.Web._1_Application.DTOs.Plants;

public class PlantCreateDto : IPlantFormData
{
    public IEnumerable<SelectListItem> AvailableCrops { get; set; } = new List<SelectListItem>();
    public string Name { get; set; } = string.Empty;
    public int CropId { get; set; }
    public PlantStatus? Status { get; set; }
    public ExperimentalGroupType? ExperimentalGroup { get; set; }
    public IEnumerable<SelectListItem> AvailableExperimentalGroups { get; set; } = new List<SelectListItem>();
}