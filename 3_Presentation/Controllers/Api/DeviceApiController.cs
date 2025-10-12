using System.Security.Claims;
using System.Text.Json;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Api;

/// <summary>
///     Controlador de API principal que maneja todas las comunicaciones entrantes de los dispositivos de hardware.
///     Proporciona endpoints para la activación, autenticación y envío de datos.
///     Utiliza LogContext para enriquecer todos los logs con el ID del dispositivo que realiza la petición.
/// </summary>
[Route("api/device-api")]
[ApiController]
public class DeviceApiController : ControllerBase
{
    private readonly IDataSubmissionService _dataSubmissionService;
    private readonly IDeviceService _deviceService;
    private readonly ILogger<DeviceApiController> _logger;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="DeviceApiController" />.
    /// </summary>
    public DeviceApiController(
        IDeviceService deviceService,
        IDataSubmissionService dataSubmissionService,
        ILogger<DeviceApiController> logger)
    {
        _deviceService = deviceService;
        _dataSubmissionService = dataSubmissionService;
        _logger = logger;
    }

    /// <summary>
    ///     Endpoint para la activación inicial de un dispositivo.
    ///     No requiere autenticación.
    /// </summary>
    /// <param name="requestDto">Datos de la solicitud de activación, incluyendo HardwareId, código y MAC address.</param>
    /// <returns>Un `Ok (200)` con los tokens de acceso si la activación es exitosa, o un `BadRequest (400)` si falla.</returns>
    [HttpPost("activate")]
    public async Task<IActionResult> ActivateDevice([FromBody] DeviceActivationRequestDto requestDto)
    {
        using (LogContext.PushProperty("HardwareId", requestDto.DeviceId))
        {
            if (!ModelState.IsValid) return BadRequest("Solicitud inválida.");

            var result = await _deviceService.ActivateDeviceAsync(requestDto);

            if (result.IsSuccess)
            {
                _logger.LogInformation("API de Dispositivo: {ApiEvent}", "ActivationSuccess");
                return Ok(result.Value);
            }

            _logger.LogWarning("API de Dispositivo: {ApiEvent} - Causa: {FailureReason}", "ActivationFailed",
                result.ErrorMessage);
            return BadRequest("Código de activación inválido, expirado, o el HardwareId ya está en uso.");
        }
    }

    /// <summary>
    ///     Endpoint para refrescar los tokens de autenticación de un dispositivo.
    /// </summary>
    /// <param name="requestDto">DTO que contiene el Refresh Token actual.</param>
    /// <returns>Un `Ok (200)` con el nuevo par de tokens si es exitoso, o `Unauthorized (401)` si el token no es válido.</returns>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] DeviceAuthRequestDto requestDto)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Token)) return BadRequest("Refresh token no puede ser nulo.");

        var result = await _deviceService.RefreshDeviceTokenAsync(requestDto.Token);

        if (result.IsSuccess)
        {
            _logger.LogInformation("API de Dispositivo: {ApiEvent}", "TokenRefreshSuccess");
            return Ok(result.Value);
        }

        _logger.LogWarning("API de Dispositivo: {ApiEvent} - Causa: {FailureReason}", "TokenRefreshFailed",
            result.ErrorMessage);
        return Unauthorized("Refresh token inválido o expirado.");
    }

    /// <summary>
    ///     Endpoint de prueba para verificar que un Access Token es válido.
    ///     Requiere autenticación con la política "DeviceAuthenticated".
    /// </summary>
    /// <returns>Un `Ok (200)` si el token es válido.</returns>
    [HttpPost("auth")]
    [Authorize(Policy = "DeviceAuthenticated")]
    public IActionResult AuthenticateDevice()
    {
        var deviceContext = GetDeviceIdentityFromClaims();
        if (deviceContext == null) return Unauthorized();

        using (LogContext.PushProperty("DeviceId", deviceContext.DeviceId))
        {
            _logger.LogInformation("API de Dispositivo: {ApiEvent}", "AuthenticationSuccess");
            return Ok();
        }
    }

    /// <summary>
    ///     Endpoint para que los dispositivos envíen registros de log al sistema.
    ///     Requiere autenticación.
    /// </summary>
    /// <param name="logDto">El DTO con la información del log.</param>
    /// <returns>Un `NoContent (204)` si el log se procesa correctamente.</returns>
    [HttpPost("log")]
    [Authorize(Policy = "DeviceAuthenticated")]
    public IActionResult SubmitLog([FromBody] DeviceLogRequestDto logDto)
    {
        var deviceContext = GetDeviceIdentityFromClaims();
        if (deviceContext == null) return Unauthorized();

        using (LogContext.PushProperty("DeviceId", deviceContext.DeviceId))
        using (LogContext.PushProperty("PlantId", deviceContext.PlantId))
        using (LogContext.PushProperty("CropId", deviceContext.CropId))
        using (LogContext.PushProperty("DeviceLogType", logDto.LogType.ToUpperInvariant()))
        {
            if (logDto.InternalDeviceTemperature.HasValue)
                LogContext.PushProperty("DeviceInternalTemp", logDto.InternalDeviceTemperature.Value);

            var logLevel = logDto.LogType.ToUpperInvariant() switch
            {
                "WARNING" => LogLevel.Warning,
                "ERROR" => LogLevel.Error,
                _ => LogLevel.Information
            };
            _logger.Log(logLevel, "API de Dispositivo: {ApiEvent} - Mensaje: {DeviceLogMessage}", "DeviceLogReceived",
                logDto.LogMessage);

            return NoContent();
        }
    }

    /// <summary>
    ///     Endpoint para que los dispositivos envíen datos de sensores ambientales.
    ///     Requiere autenticación.
    /// </summary>
    /// <param name="ambientDataDto">El DTO con los datos de temperatura, humedad, etc.</param>
    /// <returns>Un `NoContent (204)` si los datos se guardan con éxito.</returns>
    [HttpPost("ambient-data")]
    [Authorize(Policy = "DeviceAuthenticated")]
    public async Task<IActionResult> SubmitAmbientData([FromBody] AmbientDataDto ambientDataDto)
    {
        if (!ModelState.IsValid) return BadRequest("Payload de datos ambientales inválido.");

        var deviceContext = GetDeviceIdentityFromClaims();
        if (deviceContext == null) return Unauthorized();

        using (LogContext.PushProperty("DeviceId", deviceContext.DeviceId))
        using (LogContext.PushProperty("PlantId", deviceContext.PlantId))
        using (LogContext.PushProperty("CropId", deviceContext.CropId))
        {
            _logger.LogInformation("API de Dispositivo: {ApiEvent}", "AmbientDataReceived");
            var result = await _dataSubmissionService.SaveAmbientDataAsync(deviceContext, ambientDataDto);

            if (!result.IsSuccess)
            {
                _logger.LogError("API de Dispositivo: {ApiEvent} - Causa: {FailureReason}", "AmbientDataFailed",
                    result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }

            return NoContent();
        }
    }

    /// <summary>
    ///     Endpoint para que los dispositivos envíen datos de capturas térmicas.
    ///     Acepta un formato `multipart/form-data` con un JSON de datos y un archivo de imagen opcional.
    ///     Requiere autenticación.
    /// </summary>
    /// <param name="thermalDataJson">Una cadena JSON con las estadísticas térmicas.</param>
    /// <param name="imageFile">Un archivo de imagen RGB opcional.</param>
    /// <returns>Un `Ok (200)` si los datos se guardan con éxito.</returns>
    [HttpPost("capture-data")]
    [Authorize(Policy = "DeviceAuthenticated")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubmitCaptureData(
        [FromForm(Name = "thermal")] string thermalDataJson,
        [FromForm(Name = "image")] IFormFile? imageFile)
    {
        var deviceContext = GetDeviceIdentityFromClaims();
        if (deviceContext == null) return Unauthorized();

        using (LogContext.PushProperty("DeviceId", deviceContext.DeviceId))
        using (LogContext.PushProperty("PlantId", deviceContext.PlantId))
        using (LogContext.PushProperty("CropId", deviceContext.CropId))
        {
            _logger.LogInformation("API de Dispositivo: {ApiEvent} con imagen: {HasImageFile}", "CaptureDataReceived",
                imageFile != null);

            if (string.IsNullOrEmpty(thermalDataJson))
            {
                _logger.LogWarning("API de Dispositivo: {ApiEvent} - Causa: {FailureReason}", "CaptureDataInvalid",
                    "El JSON de datos térmicos está vacío.");
                return BadRequest("El JSON de datos térmicos está vacío.");
            }

            ThermalDataDto? thermalDataDto;
            try
            {
                thermalDataDto = JsonSerializer.Deserialize<ThermalDataDto>(thermalDataJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (thermalDataDto == null)
                {
                    _logger.LogWarning("API de Dispositivo: {ApiEvent} - Causa: {FailureReason}", "CaptureDataInvalid",
                        "No se pudo deserializar el JSON de datos térmicos.");
                    return BadRequest("No se pudo deserializar el JSON de datos térmicos.");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "API de Dispositivo: {ApiEvent} - Causa: {FailureReason}", "CaptureDataInvalid",
                    "Formato JSON inválido para los datos térmicos.");
                return BadRequest("Formato JSON inválido para los datos térmicos.");
            }

            var result = await _dataSubmissionService.SaveCaptureDataAsync(deviceContext, thermalDataDto,
                thermalDataJson, imageFile, DateTime.UtcNow);

            if (result.IsSuccess)
            {
                _logger.LogInformation("API de Dispositivo: {ApiEvent}", "CaptureDataSuccess");
                return Ok(new { message = "Datos térmicos recibidos y guardados exitosamente." });
            }

            _logger.LogError("API de Dispositivo: {ApiEvent} - Causa: {FailureReason}", "CaptureDataFailed",
                result.ErrorMessage);
            return StatusCode(500,
                new { message = "Fallo al guardar los datos térmicos.", error = result.ErrorMessage });
        }
    }

    /// <summary>
    ///     Método de utilidad para extraer la identidad del dispositivo (ID, PlantID, CropID)
    ///     a partir de los claims contenidos en el token de acceso JWT.
    /// </summary>
    /// <returns>Un objeto <c>DeviceIdentityContext</c> con los IDs, o null si los claims no son válidos.</returns>
    private DeviceIdentityContext? GetDeviceIdentityFromClaims()
    {
        var deviceIdClaim = User.FindFirstValue("DeviceId");
        if (string.IsNullOrEmpty(deviceIdClaim) || !int.TryParse(deviceIdClaim, out var deviceId) || deviceId <= 0)
        {
            _logger.LogError("API de Dispositivo: {ApiEvent} - Causa: {FailureReason}", "AuthenticationInvalidClaim",
                "El claim 'DeviceId' está ausente o es inválido en el JWT.");
            return null;
        }

        int.TryParse(User.FindFirstValue("PlantId"), out var plantId);
        int.TryParse(User.FindFirstValue("CropId"), out var cropId);

        return new DeviceIdentityContext { DeviceId = deviceId, PlantId = plantId, CropId = cropId };
    }
}