using ArandanoIRT.Web._0_Domain.Enums;

namespace ArandanoIRT.Web._1_Application.DTOs.Device;

public class DeviceQueryFilters
{
    public int? PlantId { get; set; }
    public DeviceStatus? Status { get; set; }
    public string SortOrder { get; set; } = "asc";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
