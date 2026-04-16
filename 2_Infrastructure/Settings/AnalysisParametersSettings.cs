namespace ArandanoIRT.Web._2_Infrastructure.Settings;

public class AnalysisParametersSettings
{
    public const string SectionName = "AnalysisParameters";
    public double CwsiThresholdIncipient { get; set; } = 0.3;
    public double CwsiThresholdCritical { get; set; } = 0.5;
    public int AnalysisWindowStartHour { get; set; } = 12;
    public int AnalysisWindowEndHour { get; set; } = 15;
    public int LightIntensityThreshold { get; set; } = 600;
}