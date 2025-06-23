using ArandanoIRT.Web.Common;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
/// Abstrae la lógica para subir archivos a un proveedor de almacenamiento de objetos (Blob Storage, S3, etc.).
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Sube un archivo a un contenedor/bucket específico.
    /// </summary>
    /// <param name="file">El archivo a subir.</param>
    /// <param name="containerName">El nombre del contenedor o bucket.</param>
    /// <param name="fileName">El nombre deseado para el archivo en el almacenamiento.</param>
    /// <returns>Un Result que contiene la URL pública del archivo subido en caso de éxito.</returns>
    Task<Result<string>> UploadFileAsync(IFormFile file, string containerName, string fileName);
}