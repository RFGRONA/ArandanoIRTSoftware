namespace ArandanoIRT.Web._2_Infrastructure.Settings;

/// <summary>
///     Contiene la configuración para conectarse al servicio de almacenamiento de objetos MinIO.
///     Estas propiedades se cargan desde la sección "Minio" en appsettings.json.
/// </summary>
public class MinioSettings
{
    /// <summary>
    ///     Define el nombre de la sección en el archivo appsettings.json.
    /// </summary>
    public const string SectionName = "Minio";

    /// <summary>
    ///     La URL del endpoint del servidor MinIO.
    /// </summary>
    public string Endpoint { get; set; } = null!;

    /// <summary>
    ///     La clave de acceso para autenticarse con MinIO.
    /// </summary>
    public string AccessKey { get; set; } = null!;

    /// <summary>
    ///     La clave secreta para autenticarse con MinIO.
    /// </summary>
    public string SecretKey { get; set; } = null!;

    /// <summary>
    ///     Indica si se debe usar una conexión segura (SSL/TLS) para conectar con MinIO.
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    ///     La URL base pública desde la cual se servirán los archivos almacenados.
    /// </summary>
    public string PublicUrlBase { get; set; } = string.Empty;
}