namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class ObservationQueryFilters
{
    public int? PlantId { get; set; }
    public int? UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}