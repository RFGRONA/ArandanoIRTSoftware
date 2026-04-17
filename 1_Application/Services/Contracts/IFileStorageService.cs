using ArandanoIRT.Web._0_Domain.Common;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Abstrae la lógica para subir archivos a un proveedor de almacenamiento de objetos (como MinIO, Azure Blob Storage,
///     AWS S3, etc.).
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    ///     Sube un archivo a un contenedor/bucket específico en el servicio de almacenamiento.
    /// </summary>
    /// <param name="file">El archivo a subir, recibido desde una petición HTTP.</param>
    /// <param name="containerName">El nombre del contenedor o bucket de destino.</param>
    /// <param name="fileName">El nombre deseado para el archivo una vez almacenado.</param>
    /// <returns>Un objeto <c>Result</c> que contiene la URL pública del archivo subido en caso de éxito.</returns>
    Task<Result<string>> UploadFileAsync(IFormFile file, string containerName, string fileName);
}