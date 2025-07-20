using System.ComponentModel.DataAnnotations;
using ArandanoIRT.Web._0_Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web._1_Application.DTOs.Plants;

public class PlantEditDto : IPlantFormData
{
    [Required]
    public int Id { get; set; } // El ID de la planta a editar

    public string Name { get; set; } = string.Empty;
    public int CropId { get; set; }
    public PlantStatus? Status { get; set; }

    public IEnumerable<SelectListItem> AvailableCrops { get; set; } = new List<SelectListItem>();
}