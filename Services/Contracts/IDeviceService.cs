using ArandanoIRT.Web.Common;
using ArandanoIRT.Web.Data.DTOs.DeviceApi;

namespace ArandanoIRT.Web.Services.Contracts;

public interface IDeviceService
{
    Task<Result<DeviceActivationResponseDto>> ActivateDeviceAsync(DeviceActivationRequestDto activationRequest);
    Task<Result<DeviceAuthResponseDto>> AuthenticateDeviceAsync(string accessToken);
    Task<Result<DeviceAuthResponseDto>> RefreshDeviceTokenAsync(string refreshTokenValue);
    Task<Result<AuthenticatedDeviceDetailsDto>> ValidateTokenAndGetDeviceDetailsAsync(string accessToken);
}