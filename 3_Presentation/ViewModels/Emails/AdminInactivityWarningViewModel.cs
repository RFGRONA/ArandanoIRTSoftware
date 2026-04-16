namespace ArandanoIRT.Web._3_Presentation.ViewModels.Emails;

public class AdminInactivityWarningViewModel
{
    public string UserName { get; set; }
    public int DaysInactive { get; set; }
    public string WarningTitle { get; set; }
    public string WarningMessage { get; set; }
    public string LoginUrl { get; set; }
}