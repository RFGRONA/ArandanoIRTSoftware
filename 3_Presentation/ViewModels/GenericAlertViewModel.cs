namespace ArandanoIRT.Web._3_Presentation.ViewModels;

public class GenericAlertViewModel
{
    public string Title { get; set; }
    public string Message { get; set; }
    public string AlertTime { get; set; } = DateTime.UtcNow.ToString("o");
    public string Severity { get; set; }
}