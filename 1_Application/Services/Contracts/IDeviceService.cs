using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
using ArandanoIRT.Web.Common;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IDeviceService
{
    Task<Result<DeviceActivationResponseDto>> ActivateDeviceAsync(DeviceActivationRequestDto activationRequest);
    Task<Result<DeviceAuthResponseDto>> RefreshDeviceTokenAsync(string refreshTokenValue);
    Task<Result<AuthenticatedDeviceDetailsDto>> ValidateTokenAndGetDeviceDetailsAsync(string accessToken);
}