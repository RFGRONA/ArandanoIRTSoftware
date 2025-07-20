using ArandanoIRT.Web._0_Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

// Para SelectListItem

namespace ArandanoIRT.Web._1_Application.DTOs.Plants;

public class PlantCreateDto : IPlantFormData
{
    public string Name { get; set; } = string.Empty;
    public int CropId { get; set; }
    public PlantStatus? Status { get; set; }

    // Para poblar los DropDownLists en la vista
    public IEnumerable<SelectListItem> AvailableCrops { get; set; } = new List<SelectListItem>();
}