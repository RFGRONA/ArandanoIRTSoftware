namespace ArandanoIRT.Web._2_Infrastructure.Settings;

/// <summary>
///     Contiene la configuración para el servicio de envío de correos Brevo.
///     Estas propiedades se cargan desde la sección "BrevoSettings" en appsettings.json.
/// </summary>
public class BrevoSettings
{
    /// <summary>
    ///     Define el nombre de la sección en el archivo appsettings.json.
    /// </summary>
    public const string SectionName = "BrevoSettings";

    /// <summary>
    ///     La clave de API para autenticarse con el servicio de Brevo.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}