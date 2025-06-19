using ArandanoIRT.Web.Common;
using ArandanoIRT.Web.Data.DTOs.DeviceApi;

namespace ArandanoIRT.Web.Services.Contracts;

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

    Task<Result> SaveDeviceLogAsync(DeviceIdentityContext deviceContext, DeviceLogEntryDto logEntry);
}