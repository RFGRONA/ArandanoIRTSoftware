namespace ArandanoIRT.Web._2_Infrastructure.Settings;

/// <summary>
///     Contiene los parámetros por defecto para el análisis de estrés hídrico.
///     Estas propiedades se cargan desde la sección "AnalysisParameters" en appsettings.json.
/// </summary>
public class AnalysisParametersSettings
{
    /// <summary>
    ///     Define el nombre de la sección en el archivo appsettings.json.
    /// </summary>
    public const string SectionName = "AnalysisParameters";

    /// <summary>
    ///     El umbral de CWSI a partir del cual se considera estrés incipiente.
    /// </summary>
    public double CwsiThresholdIncipient { get; set; } = 0.3;

    /// <summary>
    ///     El umbral de CWSI a partir del cual se considera estrés crítico.
    /// </summary>
    public double CwsiThresholdCritical { get; set; } = 0.5;

    /// <summary>
    ///     La hora de inicio (formato 24h) de la ventana de tiempo para realizar análisis.
    /// </summary>
    public int AnalysisWindowStartHour { get; set; } = 12;

    /// <summary>
    ///     La hora de fin (formato 24h) de la ventana de tiempo para realizar análisis.
    /// </summary>
    public int AnalysisWindowEndHour { get; set; } = 15;

    /// <summary>
    ///     El umbral mínimo de intensidad lumínica para considerar válidas las condiciones de análisis.
    /// </summary>
    public int LightIntensityThreshold { get; set; } = 600;
}