namespace ArandanoIRT.Web._0_Domain.Common;

/// <summary>
/// Proporciona métodos de extensión para la manipulación y conversión de objetos DateTime,
/// enfocados principalmente en la zona horaria de Colombia.
/// </summary>
public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo ColombiaTimeZone;

    /// <summary>
    /// Inicializa estáticamente la zona horaria de Colombia, con soporte para sistemas Linux y Windows.
    /// Si no se encuentra la zona horaria, se utiliza UTC como respaldo.
    /// </summary>
    static DateTimeExtensions()
    {
        try
        {
            // Para sistemas Linux
            ColombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Para sistemas Windows
                ColombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch
            {
                // Fallback a UTC si no se encuentra ninguna zona horaria
                ColombiaTimeZone = TimeZoneInfo.Utc;
            }
        }
    }

    /// <summary>
    /// Convierte un objeto DateTime a la hora local de Colombia.
    /// Maneja correctamente fechas de tipo Utc, Local y Unspecified.
    /// </summary>
    /// <param name="dateTimeToConvert">El DateTime que se va a convertir.</param>
    /// <returns>El DateTime en la hora local de Colombia.</returns>
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
                // Asume que el tiempo no especificado es local como valor predeterminado seguro.
                utcDateTime = DateTime.SpecifyKind(dateTimeToConvert, DateTimeKind.Local).ToUniversalTime();
                break;
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, ColombiaTimeZone);
    }

    /// <summary>
    /// Convierte un objeto DateTime nullable a la hora local de Colombia.
    /// </summary>
    /// <param name="utcDateTime">El DateTime nullable que se va a convertir.</param>
    /// <returns>Un DateTime nullable en la hora local de Colombia, o null si la entrada es null.</returns>
    public static DateTime? ToColombiaTime(this DateTime? utcDateTime)
    {
        if (!utcDateTime.HasValue)
            return null;

        return utcDateTime.Value.ToColombiaTime();
    }

    /// <summary>
    /// Verifica si un DateTime (en UTC) se encuentra dentro de una ventana de tiempo específica (hora de inicio y fin) en la hora local de Colombia.
    /// </summary>
    /// <param name="utcNow">La hora UTC actual a verificar.</param>
    /// <param name="startHour">La hora de inicio de la ventana (inclusiva).</param>
    /// <param name="endHour">La hora de fin de la ventana (exclusiva).</param>
    /// <returns>True si la hora está dentro de la ventana, de lo contrario, false.</returns>
    public static bool IsWithinColombiaTimeWindow(this DateTime utcNow, int startHour, int endHour)
    {
        var colombiaTime = utcNow.ToColombiaTime();
        return colombiaTime.Hour >= startHour && colombiaTime.Hour < endHour;
    }

    /// <summary>
    /// Convierte de forma segura un DateTime a la hora universal coordinada (UTC).
    /// Si el DateTime ya es UTC, lo devuelve sin cambios.
    /// Si es Local o Unspecified, asume que es hora de Colombia y lo convierte a UTC.
    /// </summary>
    /// <param name="dt">El DateTime a convertir.</param>
    /// <returns>El DateTime equivalente en UTC.</returns>
    public static DateTime ToSafeUniversalTime(this DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc)
        {
            return dt;
        }

        var colombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

        // Se fuerza el Kind a Unspecified para una conversión segura y predecible.
        var unspecifiedDateTime = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);

        return TimeZoneInfo.ConvertTimeToUtc(unspecifiedDateTime, colombiaTimeZone);
    }
}