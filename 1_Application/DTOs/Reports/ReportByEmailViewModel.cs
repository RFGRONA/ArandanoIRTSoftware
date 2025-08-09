using ArandanoIRT.Web._0_Domain.Common;

namespace ArandanoIRT.Web._1_Application.DTOs.Reports;

public class ReportByEmailViewModel
{
    public string PlantName { get; set; }
    public DateTime? GenerationDate { get; set; } = DateTime.UtcNow;
}