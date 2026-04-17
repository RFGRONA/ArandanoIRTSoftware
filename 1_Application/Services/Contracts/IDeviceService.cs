using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para el servicio que maneja la lógica de negocio orientada a los dispositivos.
///     Se encarga de la activación, autenticación y gestión del estado de los dispositivos desde la perspectiva de la API.
/// </summary>
public interface IDeviceService
{
    /// <summary>
    ///     Procesa la solicitud de activación de un nuevo dispositivo, validando el código y la MAC Address.
    ///     Si es exitoso, genera los primeros tokens de autenticación.
    /// </summary>
    /// <param name="activationRequest">El DTO con los datos de la solicitud de activación.</param>
    /// <returns>
    ///     Un objeto <c>Result</c> que contiene el <c>DeviceActivationResponseDto</c> con los tokens si la operación fue
    ///     exitosa.
    /// </returns>
    Task<Result<DeviceActivationResponseDto>> ActivateDeviceAsync(DeviceActivationRequestDto activationRequest);

    /// <summary>
    ///     Refresca los tokens de autenticación de un dispositivo utilizando un Refresh Token válido.
    /// </summary>
    /// <param name="refreshTokenValue">El valor del Refresh Token enviado por el dispositivo.</param>
    /// <returns>Un objeto <c>Result</c> que contiene el <c>DeviceAuthResponseDto</c> con los nuevos tokens.</returns>
    Task<Result<DeviceAuthResponseDto>> RefreshDeviceTokenAsync(string refreshTokenValue);

    /// <summary>
    ///     Valida un Access Token proporcionado por un dispositivo y, si es válido, devuelve los detalles de identidad del
    ///     dispositivo.
    /// </summary>
    /// <param name="accessToken">El Access Token a validar.</param>
    /// <returns>Un objeto <c>Result</c> que contiene el <c>AuthenticatedDeviceDetailsDto</c> con la identidad del dispositivo.</returns>
    Task<Result<AuthenticatedDeviceDetailsDto>> ValidateTokenAndGetDeviceDetailsAsync(string accessToken);

    /// <summary>
    ///     Obtiene una lista de dispositivos que se consideran inactivos según un umbral de tiempo.
    /// </summary>
    /// <param name="inactivityMultiplier">Un multiplicador del intervalo de recolección para definir el umbral de inactividad.</param>
    /// <returns>Una lista de entidades <c>Device</c> que están inactivas.</returns>
    Task<List<Device>> GetInactiveDevicesAsync(int inactivityMultiplier);

    /// <summary>
    ///     Actualiza el estado operativo de un dispositivo (ej. de Activo a Inactivo).
    /// </summary>
    /// <param name="deviceId">El ID del dispositivo a actualizar.</param>
    /// <param name="newStatus">El nuevo estado a asignar.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la operación fue exitosa.</returns>
    Task<Result> UpdateDeviceStatusAsync(int deviceId, DeviceStatus newStatus);
}