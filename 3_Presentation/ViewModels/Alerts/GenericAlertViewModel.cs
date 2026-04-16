namespace ArandanoIRT.Web._3_Presentation.ViewModels.Alerts;

public class GenericAlertViewModel
{
    public string Title { get; set; }
    public string Message { get; set; }
    public DateTime? AlertTime { get; set; } = DateTime.UtcNow;
    public string Severity { get; set; }
}