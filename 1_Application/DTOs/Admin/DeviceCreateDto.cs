using ArandanoIRT.Web._0_Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering; // Para SelectListItem

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class DeviceCreateDto : IDeviceFormData
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PlantId { get; set; }
    public short DataCollectionIntervalMinutes { get; set; } = 15; // Default
    public DeviceStatus Status { get; set; }
    public string? MacAddress { get; set; }

    // Para poblar los DropDownLists en la vista
    public IEnumerable<SelectListItem> AvailablePlants { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> AvailableStatuses { get; set; } = new List<SelectListItem>();
}