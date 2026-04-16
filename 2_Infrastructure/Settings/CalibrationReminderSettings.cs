namespace ArandanoIRT.Web._2_Infrastructure.Settings;

public class CalibrationReminderSettings
{
    public const string SectionName = "CalibrationReminder";
    public int ReminderIntervalMonths { get; set; } = 3;
}