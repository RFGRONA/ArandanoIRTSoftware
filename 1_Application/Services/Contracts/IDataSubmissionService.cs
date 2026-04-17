using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para el servicio de recepción de datos.
///     Es responsable de procesar y guardar los datos enviados por los dispositivos de hardware a la API.
/// </summary>
public interface IDataSubmissionService
{
    /// <summary>
    ///     Guarda los datos de los sensores ambientales (temperatura, humedad, etc.) en la base de datos.
    /// </summary>
    /// <param name="deviceContext">El contexto de identidad del dispositivo que envía los datos.</param>
    /// <param name="ambientData">El DTO con los datos ambientales a guardar.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la operación fue exitosa.</returns>
    Task<Result> SaveAmbientDataAsync(DeviceIdentityContext deviceContext, AmbientDataDto ambientData);

    /// <summary>
    ///     Guarda los datos de una captura termográfica, incluyendo las estadísticas y la imagen RGB asociada.
    /// </summary>
    /// <param name="deviceContext">El contexto de identidad del dispositivo que envía los datos.</param>
    /// <param name="thermalData">El DTO con las estadísticas térmicas deserializadas.</param>
    /// <param name="thermalDataJsonString">La cadena JSON original para almacenar en la base de datos.</param>
    /// <param name="imageFile">El archivo de imagen RGB enviado por el dispositivo.</param>
    /// <param name="recordedAtServer">La marca de tiempo del servidor cuando se recibieron los datos.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la operación fue exitosa.</returns>
    Task<Result> SaveCaptureDataAsync(
        DeviceIdentityContext deviceContext,
        ThermalDataDto thermalData, // La parte JSON deserializada
        string thermalDataJsonString, // El string JSON original para guardar en JSONB
        IFormFile imageFile, // El archivo de imagen RGB
        DateTime recordedAtServer // Timestamp del servidor
    );
}