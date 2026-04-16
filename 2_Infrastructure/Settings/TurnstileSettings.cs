namespace ArandanoIRT.Web._2_Infrastructure.Settings;

public class TurnstileSettings
{
    public const string SectionName = "TurnstileSettings";

    public string SiteKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}