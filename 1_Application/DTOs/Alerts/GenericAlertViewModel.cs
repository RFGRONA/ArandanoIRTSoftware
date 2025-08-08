namespace ArandanoIRT.Web._1_Application.DTOs.Alerts;

public class GenericAlertViewModel
{
    public string Title { get; set; }
    public string Message { get; set; }
    public string AlertTime { get; set; } = DateTime.UtcNow.ToString("o");
    public string Severity { get; set; }
}