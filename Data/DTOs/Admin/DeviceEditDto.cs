using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web.Data.DTOs.Admin;

public class DeviceEditDto : IDeviceFormData
{
    [Required]
    public int Id { get; set; } // El ID del dispositivo a editar

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PlantId { get; set; }
    public short DataCollectionTimeMinutes { get; set; }
    public int StatusId { get; set; }

    // Para poblar los DropDownLists en la vista
    public IEnumerable<SelectListItem> AvailablePlants { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> AvailableStatuses { get; set; } = new List<SelectListItem>();
}