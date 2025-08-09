namespace ArandanoIRT.Web._1_Application.DTOs.Alerts;

public class GenericAlertViewModel
{
    public string Title { get; set; }
    public string Message { get; set; }
    public DateTime? AlertTime { get; set; } = DateTime.UtcNow;
    public string Severity { get; set; }
}