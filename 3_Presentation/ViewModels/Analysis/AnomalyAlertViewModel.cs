namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

public class AnomalyAlertViewModel
{
    public string UserName { get; set; }
    public string PlantName { get; set; }
    public int PlantId { get; set; }
    public DateTime? AlertTime { get; set; } = DateTime.UtcNow;
}