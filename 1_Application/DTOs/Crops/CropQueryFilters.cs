namespace ArandanoIRT.Web._1_Application.DTOs.Crops;

public class CropQueryFilters
{
    public string? CityName { get; set; }
    public string SortOrder { get; set; } = "asc";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
