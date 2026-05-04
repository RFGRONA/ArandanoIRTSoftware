namespace ArandanoIRT.Web._2_Infrastructure.Settings;

/// <summary>
///     Contiene la configuración para la generación y validación de tokens de autenticación de dispositivos.
///     Estas propiedades se cargan desde la sección "TokenSettings" en appsettings.json.
/// </summary>
public class TokenSettings
{
    /// <summary>
    ///     Define el nombre de la sección en el archivo appsettings.json.
    /// </summary>
    public const string SectionName = "TokenSettings";

    /// <summary>
    ///     La duración en minutos de un token de acceso (Access Token).
    /// </summary>
    public int AccessTokenDurationMinutes { get; set; } = 60;

    /// <summary>
    ///     La duración en días de un token de refresco (Refresh Token).
    /// </summary>
    public int RefreshTokenDurationDays { get; set; } = 7;

    /// <summary>
    ///     El umbral en minutos antes de la expiración de un token de acceso para marcarlo como "requiere refresco".
    /// </summary>
    public int AccessTokenNearExpiryThresholdMinutes { get; set; } = 5;

    /// <summary>
    ///     La duración en días de la validez de un código de activación de dispositivo.
    /// </summary>
    public int ActivationCodeExpirationInDays { get; set; } = 1;
}