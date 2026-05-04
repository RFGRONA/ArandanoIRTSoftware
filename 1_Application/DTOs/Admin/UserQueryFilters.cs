namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class UserQueryFilters
{
    public string? Role { get; set; }
    public string SortOrder { get; set; } = "asc";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
