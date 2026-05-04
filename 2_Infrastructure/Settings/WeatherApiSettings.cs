namespace ArandanoIRT.Web._2_Infrastructure.Settings;

/// <summary>
///     Contiene la configuración para conectarse al servicio externo de clima (WeatherAPI).
///     Estas propiedades se cargan desde la sección "WeatherApi" en appsettings.json.
/// </summary>
public class WeatherApiSettings
{
    /// <summary>
    ///     Define el nombre de la sección en el archivo appsettings.json.
    /// </summary>
    public const string SectionName = "WeatherApi";

    /// <summary>
    ///     La clave de API para autenticarse con el servicio de WeatherAPI.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    ///     La URL base del servicio de WeatherAPI.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}