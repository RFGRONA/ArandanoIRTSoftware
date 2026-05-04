namespace ArandanoIRT.Web._2_Infrastructure.Settings;

/// <summary>
///     Contiene las configuraciones para los servicios que se ejecutan en segundo plano.
///     Estas propiedades se cargan desde la sección "BackgroundJobs" en appsettings.json.
/// </summary>
public class BackgroundJobSettings
{
    /// <summary>
    ///     Define el nombre de la sección en el archivo appsettings.json.
    /// </summary>
    public const string SectionName = "BackgroundJobs";

    /// <summary>
    ///     El intervalo en minutos con el que el servicio de inactividad de dispositivos se ejecuta.
    /// </summary>
    public int InactivityCheckIntervalMinutes { get; set; } = 15;

    /// <summary>
    ///     El intervalo en minutos con el que el servicio de análisis de estrés hídrico se ejecuta.
    /// </summary>
    public int AnalysisIntervalMinutes { get; set; } = 15;
}