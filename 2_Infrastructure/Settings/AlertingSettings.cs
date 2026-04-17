namespace ArandanoIRT.Web._2_Infrastructure.Settings;

/// <summary>
///     Contiene las configuraciones relacionadas con el sistema de alertas.
///     Estas propiedades se cargan desde la sección "Alerting" en appsettings.json.
/// </summary>
public class AlertingSettings
{
    /// <summary>
    ///     Define el nombre de la sección en el archivo appsettings.json.
    /// </summary>
    public const string SectionName = "Alerting";

    /// <summary>
    ///     La clave de API para integrarse con servicios de alerta como Grafana.
    /// </summary>
    public string GrafanaApiKey { get; set; } = string.Empty;

    /// <summary>
    ///     Multiplicador usado sobre el intervalo de recolección de un dispositivo para determinar cuándo se considera
    ///     inactivo.
    /// </summary>
    public int InactivityCheckMultiplier { get; set; } = 4;
}