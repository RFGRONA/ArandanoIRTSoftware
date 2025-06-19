using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering; // Para SelectListItem

namespace ArandanoIRT.Web.Data.DTOs.Admin;

public class PlantCreateDto : IPlantFormData
{
    public string Name { get; set; } = string.Empty;
    public int CropId { get; set; }
    public int StatusId { get; set; }

    // Para poblar los DropDownLists en la vista
    public IEnumerable<SelectListItem> AvailableCrops { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> AvailableStatuses { get; set; } = new List<SelectListItem>();
}