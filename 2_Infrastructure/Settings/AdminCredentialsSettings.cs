namespace ArandanoIRT.Web._2_Infrastructure.Settings;

/// <summary>
///     Representa las credenciales para el usuario administrador "bootstrap" o de arranque.
///     Estas propiedades se cargan desde la sección "AdminCredentials" en appsettings.json.
/// </summary>
public class AdminCredentialsSettings
{
    /// <summary>
    ///     Define el nombre de la sección en el archivo appsettings.json.
    /// </summary>
    public const string SectionName = "AdminCredentials";

    /// <summary>
    ///     El nombre de usuario para la cuenta de administrador de arranque.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    ///     El hash de la contraseña para la cuenta de administrador de arranque.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
}