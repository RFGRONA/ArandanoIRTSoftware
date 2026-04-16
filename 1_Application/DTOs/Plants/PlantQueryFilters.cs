using ArandanoIRT.Web._0_Domain.Enums;

namespace ArandanoIRT.Web._1_Application.DTOs.Plants;

public class PlantQueryFilters
{
    public int? CropId { get; set; }
    public PlantStatus? Status { get; set; }
    public string SortOrder { get; set; } = "asc";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
