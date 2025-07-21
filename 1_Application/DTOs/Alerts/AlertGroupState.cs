namespace ArandanoIRT.Web._1_Application.DTOs.Alerts;

public class AlertGroupState
{
    public int Count { get; set; } = 1;
    public DateTime FirstAlertTimestamp { get; set; } = DateTime.UtcNow;
    public string Summary { get; set; }
}