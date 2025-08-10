using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IDataSubmissionService
{
    Task<Result> SaveAmbientDataAsync(DeviceIdentityContext deviceContext, AmbientDataDto ambientData);

    Task<Result> SaveCaptureDataAsync(
        DeviceIdentityContext deviceContext,
        ThermalDataDto thermalData, // La parte JSON deserializada
        string thermalDataJsonString, // El string JSON original para guardar en JSONB
        IFormFile imageFile, // El archivo de imagen RGB
        DateTime recordedAtServer // Timestamp del servidor
    );
}