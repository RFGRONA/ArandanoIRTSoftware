using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IDeviceService
{
    Task<Result<DeviceActivationResponseDto>> ActivateDeviceAsync(DeviceActivationRequestDto activationRequest);
    Task<Result<DeviceAuthResponseDto>> RefreshDeviceTokenAsync(string refreshTokenValue);
    Task<Result<AuthenticatedDeviceDetailsDto>> ValidateTokenAndGetDeviceDetailsAsync(string accessToken);
    Task<List<Device>> GetInactiveDevicesAsync(int inactivityMultiplier);
    Task<Result> UpdateDeviceStatusAsync(int deviceId, DeviceStatus newStatus);
}