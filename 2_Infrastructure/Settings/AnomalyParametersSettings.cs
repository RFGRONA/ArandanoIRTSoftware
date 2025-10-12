namespace ArandanoIRT.Web._2_Infrastructure.Settings;

/// <summary>
///     Contiene los parámetros por defecto para la detección de anomalías en los datos.
///     Estas propiedades se cargan desde la sección "AnomalyParameters" en appsettings.json.
/// </summary>
public class AnomalyParametersSettings
{
    /// <summary>
    ///     Define el nombre de la sección en el archivo appsettings.json.
    /// </summary>
    public const string SectionName = "AnomalyParameters";

    /// <summary>
    ///     La diferencia de temperatura (T. Canopia - T. Ambiente) a partir de la cual se considera una anomalía.
    /// </summary>
    public double DeltaTThreshold { get; set; } = 1.5;

    /// <summary>
    ///     El tiempo mínimo en minutos que una condición anómala debe persistir para ser considerada una alerta.
    /// </summary>
    public int DurationMinutes { get; set; } = 30;
}