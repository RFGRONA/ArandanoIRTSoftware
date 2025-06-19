namespace ArandanoIRT.Web.Common;

public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo ColombiaTimeZone;
    // Podrías inyectar ILogger aquí si quieres loguear errores de la extensión,
    // pero para una extensión estática, tendrías que pasarlo como parámetro o usar un logger estático.

    static DateTimeExtensions()
    {
        try
        {
            ColombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        }
        catch (TimeZoneNotFoundException)
        {
            // Loggear este error críticamente si tienes un logger estático disponible
            // Console.WriteLine("FATAL: TimeZone 'America/Bogota' not found. Falling back to SA Pacific Standard Time.");
            try
            {
                 ColombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch
            {
                // Console.WriteLine("FATAL: Fallback TimeZone 'SA Pacific Standard Time' also not found. Using UTC as last resort for ColombiaTimeZone.");
                ColombiaTimeZone = TimeZoneInfo.Utc; // Como último recurso muy problemático.
            }
        }
    }

    public static DateTime ToColombiaTime(this DateTime dateTimeToConvert)
    {
        DateTime utcDateTime;

        switch (dateTimeToConvert.Kind)
        {
            case DateTimeKind.Utc:
                // Ya es UTC, lista para convertir a Colombia.
                utcDateTime = dateTimeToConvert;
                // Console.WriteLine($"ToColombiaTime: Input Kind Utc ('{dateTimeToConvert:o}'), using as is for UTC base.");
                break;

            case DateTimeKind.Local:
                // Es Local (basada en la zona del servidor de la aplicación). Convertir a UTC.
                utcDateTime = dateTimeToConvert.ToUniversalTime();
                // Console.WriteLine($"ToColombiaTime: Input Kind Local ('{dateTimeToConvert:o}'), converted to UTC '{utcDateTime:o}'.");
                break;

            case DateTimeKind.Unspecified:
            default:
                // TRATAMIENTO CLAVE:
                // Asumimos que 'Unspecified' de Supabase (en este contexto de la aplicación)
                // representa un DateTime que YA ESTÁ en la hora local del servidor de la aplicación.
                // Por lo tanto, lo tratamos como 'Local' para convertirlo a UTC correctamente.
                // Console.WriteLine($"ToColombiaTime: Input Kind Unspecified ('{dateTimeToConvert:o}'), treating as Local Server Time and converting to UTC.");
                utcDateTime = DateTime.SpecifyKind(dateTimeToConvert, DateTimeKind.Local).ToUniversalTime();
                break;
        }

        // Ahora utcDateTime es definitivamente UTC. Procedemos a convertir a la zona de Colombia.
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, ColombiaTimeZone);
        }
        catch (Exception ex) // Captura más general por si ColombiaTimeZone no se inicializó bien.
        {
            // Loggear el error
            // Console.WriteLine($"Error converting '{utcDateTime:o}' (UTC) to Colombia Time: {ex.Message}. Returning original UTC DateTime.");
            return utcDateTime; // Fallback: devuelve la fecha UTC que se intentó convertir.
        }
    }

    // Versión para Nullable<DateTime>
    public static DateTime? ToColombiaTime(this DateTime? utcDateTime)
    {
        if (!utcDateTime.HasValue)
            return null;

        return utcDateTime.Value.ToColombiaTime();
    }
}