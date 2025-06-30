using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

// DTO para el resultado de la creación de un dispositivo, incluyendo el código de activación.
public class DeviceCreationResultDto
{
    public int DeviceId { get; set; }
    public string ActivationCode { get; set; } = string.Empty;
    public DateTime ActivationCodeExpiresAt { get; set; }
}

public interface IDeviceAdminService
{
    Task<Result<IEnumerable<DeviceSummaryDto>>> GetAllDevicesAsync();
    Task<Result<DeviceDetailsDto?>> GetDeviceByIdAsync(int deviceId);
    Task<Result<DeviceEditDto?>> GetDeviceForEditByIdAsync(int deviceId);
    Task<Result<DeviceCreationResultDto>> CreateDeviceAsync(DeviceCreateDto deviceDto);
    Task<Result> UpdateDeviceAsync(DeviceEditDto deviceDto);
    Task<Result> DeleteDeviceAsync(int deviceId); // Cuidado con las dependencias

    // Métodos para poblar dropdowns
    IEnumerable<SelectListItem> GetDeviceStatusesForSelection();
    Task<IEnumerable<SelectListItem>> GetPlantsForSelectionAsync();
}