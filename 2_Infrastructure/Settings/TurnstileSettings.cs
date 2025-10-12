namespace ArandanoIRT.Web._2_Infrastructure.Settings;

/// <summary>
///     Contiene la configuración para el servicio de protección anti-bot Cloudflare Turnstile.
///     Estas propiedades se cargan desde la sección "TurnstileSettings" en appsettings.json.
/// </summary>
public class TurnstileSettings
{
    /// <summary>
    ///     Define el nombre de la sección en el archivo appsettings.json.
    /// </summary>
    public const string SectionName = "TurnstileSettings";

    /// <summary>
    ///     La clave del sitio (Site Key) de Turnstile, usada en el lado del cliente (frontend).
    /// </summary>
    public string SiteKey { get; set; } = string.Empty;

    /// <summary>
    ///     La clave secreta (Secret Key) de Turnstile, usada en el lado del servidor para la validación.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;
}