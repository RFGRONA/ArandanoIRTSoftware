namespace ArandanoIRT.Web._2_Infrastructure.Settings;

public class AnomalyParametersSettings
{
    public const string SectionName = "AnomalyParameters";
    public double DeltaTThreshold { get; set; } = 1.5;
    public int DurationMinutes { get; set; } = 30;
}