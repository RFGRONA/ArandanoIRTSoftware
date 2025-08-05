namespace ArandanoIRT.Web._3_Presentation.ViewModels;

public class FailedLoginAlertViewModel
{
    public string? UserName { get; set; }
    public DateTime AlertTime { get; set; }
    public string? UserEmail { get; set; }
    public string? ForgotPasswordUrl { get; set; }
}
