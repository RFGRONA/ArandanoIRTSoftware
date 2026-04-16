namespace ArandanoIRT.Web._1_Application.DTOs.Alerts;

public class GrafanaWebhookPayload
{
    public string? AlertName { get; set; }
    public string? Message { get; set; }
    public string? Level { get; set; }
    public Dictionary<string, string> CommonLabels { get; set; } = new();
}