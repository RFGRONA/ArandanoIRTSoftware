namespace ArandanoIRT.Web._1_Application.DTOs.Alerts;

/// <summary>
/// Representa la estructura de datos (payload) que envía Grafana a través de un webhook cuando se dispara una alerta.
/// </summary>
public class GrafanaWebhookPayload
{
    public string? AlertName { get; set; }
    public string? Message { get; set; }
    public string? Level { get; set; }
    public Dictionary<string, string> CommonLabels { get; set; } = new();
}