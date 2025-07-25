namespace ArandanoIRT.Web._0_Domain.Common;

public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo ColombiaTimeZone;

    static DateTimeExtensions()
    {
        try
        {
            // For Linux systems
            ColombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // For Windows systems
                ColombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch
            {
                // Fallback to UTC if no timezone is found
                ColombiaTimeZone = TimeZoneInfo.Utc;
            }
        }
    }

    /// <summary>
    ///     Converts a DateTime object to Colombia local time.
    ///     It handles UTC, Local, and Unspecified kinds.
    /// </summary>
    /// <param name="dateTimeToConvert">The DateTime to convert.</param>
    /// <returns>The DateTime in Colombian local time.</returns>
    public static DateTime ToColombiaTime(this DateTime dateTimeToConvert)
    {
        DateTime utcDateTime;

        switch (dateTimeToConvert.Kind)
        {
            case DateTimeKind.Utc:
                utcDateTime = dateTimeToConvert;
                break;

            case DateTimeKind.Local:
                utcDateTime = dateTimeToConvert.ToUniversalTime();
                break;

            case DateTimeKind.Unspecified:
            default:
                // Assume Unspecified time is Local time as a safe default
                utcDateTime = DateTime.SpecifyKind(dateTimeToConvert, DateTimeKind.Local).ToUniversalTime();
                break;
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, ColombiaTimeZone);
    }

    /// <summary>
    ///     Converts a nullable DateTime object to Colombia local time.
    /// </summary>
    /// <param name="utcDateTime">The nullable DateTime to convert.</param>
    /// <returns>A nullable DateTime in Colombian local time, or null.</returns>
    public static DateTime? ToColombiaTime(this DateTime? utcDateTime)
    {
        if (!utcDateTime.HasValue)
            return null;

        return utcDateTime.Value.ToColombiaTime();
    }

    /// <summary>
    ///     Checks if a given UTC DateTime falls within a specific time window (start and end hour) in Colombian local time.
    /// </summary>
    /// <param name="utcNow">The current UTC time to check.</param>
    /// <param name="startHour">The start hour of the window (inclusive).</param>
    /// <param name="endHour">The end hour of the window (exclusive).</param>
    /// <returns>True if the time is within the window, false otherwise.</returns>
    public static bool IsWithinColombiaTimeWindow(this DateTime utcNow, int startHour, int endHour)
    {
        var colombiaTime = utcNow.ToColombiaTime();
        return colombiaTime.Hour >= startHour && colombiaTime.Hour < endHour;
    }
}