using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web.Data.DTOs.Admin;

public class PlantEditDto : IPlantFormData
{
    [Required]
    public int Id { get; set; } // El ID de la planta a editar

    public string Name { get; set; } = string.Empty;
    public int CropId { get; set; }
    public int StatusId { get; set; }

    // Para poblar los DropDownLists en la vista
    public IEnumerable<SelectListItem> AvailableCrops { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> AvailableStatuses { get; set; } = new List<SelectListItem>();
}