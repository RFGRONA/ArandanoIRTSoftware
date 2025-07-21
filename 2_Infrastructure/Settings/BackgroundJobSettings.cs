namespace ArandanoIRT.Web._2_Infrastructure.Settings;

public class BackgroundJobSettings
{
    public const string SectionName = "BackgroundJobs";

    public int InactivityCheckIntervalMinutes { get; set; } = 15;
}