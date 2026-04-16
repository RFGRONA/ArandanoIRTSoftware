namespace ArandanoIRT.Web._2_Infrastructure.Settings;

public class AlertingSettings
{
    public const string SectionName = "Alerting";
    public string GrafanaApiKey { get; set; } = string.Empty;
    public int InactivityCheckMultiplier { get; set; } = 4;
}