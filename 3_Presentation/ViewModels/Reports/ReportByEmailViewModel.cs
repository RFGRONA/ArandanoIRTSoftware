namespace ArandanoIRT.Web._3_Presentation.ViewModels.Reports;

public class ReportByEmailViewModel
{
    public string PlantName { get; set; }
    public DateTime? GenerationDate { get; set; } = DateTime.UtcNow;
}