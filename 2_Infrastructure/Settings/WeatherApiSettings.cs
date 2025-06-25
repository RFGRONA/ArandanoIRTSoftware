namespace ArandanoIRT.Web._2_Infrastructure.Settings;

public class WeatherApiSettings
{
    public const string SectionName = "WeatherApi";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}