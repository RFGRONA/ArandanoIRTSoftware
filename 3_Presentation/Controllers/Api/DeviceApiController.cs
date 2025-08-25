using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Serilog.Context;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Api;

[Route("api/device-api")]
[ApiController]
public class DeviceApiController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IDataSubmissionService _dataSubmissionService;
    private readonly ILogger<DeviceApiController> _logger;

    public DeviceApiController(
        IDeviceService deviceService,
        IDataSubmissionService dataSubmissionService,
        ILogger<DeviceApiController> logger)
    {
        _deviceService = deviceService;
        _dataSubmissionService = dataSubmissionService;
        _logger = logger;
    }

    [HttpPost("activate")]
    public async Task<IActionResult> ActivateDevice([FromBody] DeviceActivationRequestDto requestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Solicitud inválida.");
        }

        var result = await _deviceService.ActivateDeviceAsync(requestDto);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Dispositivo con HardwareId {HardwareId} activado exitosamente.", requestDto.DeviceId);
            return Ok(result.Value);
        }

        _logger.LogWarning("Fallo en la activación para HardwareId {HardwareId}. Error: {ErrorMessage}", requestDto.DeviceId, result.ErrorMessage);
        return BadRequest("Código de activación inválido, expirado, o el HardwareId ya está en uso.");
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] DeviceAuthRequestDto requestDto)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Token))
        {
            return BadRequest("Refresh token no puede ser nulo.");
        }

        var result = await _deviceService.RefreshDeviceTokenAsync(requestDto.Token);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        _logger.LogWarning("Fallo en refresco de token. Error: {ErrorMessage}", result.ErrorMessage);
        return Unauthorized("Refresh token inválido o expirado.");
    }

    [HttpPost("auth")]
    [Authorize(Policy = "DeviceAuthenticated")]
    public IActionResult AuthenticateDevice()
    {
        var deviceContext = GetDeviceIdentityFromClaims();
        if (deviceContext == null) return Unauthorized();

        using (LogContext.PushProperty("DeviceId", deviceContext.DeviceId))
        {
            _logger.LogInformation("Check de autenticación exitoso.");
            return Ok(new { status = "authenticated" });
        }
    }

    [HttpPost("log")]
    [Authorize(Policy = "DeviceAuthenticated")]
    public IActionResult SubmitLog([FromBody] DeviceLogRequestDto logDto)
    {
        var deviceContext = GetDeviceIdentityFromClaims();
        if (deviceContext == null) return Unauthorized();

        using (LogContext.PushProperty("DeviceId", deviceContext.DeviceId))
        using (LogContext.PushProperty("PlantId", deviceContext.PlantId))
        using (LogContext.PushProperty("CropId", deviceContext.CropId))
        {
            var logLevel = logDto.LogType.ToUpperInvariant() switch
            {
                "WARNING" => LogLevel.Warning,
                "ERROR" => LogLevel.Error,
                _ => LogLevel.Information,
            };

            _logger.Log(logLevel,
                "Log recibido desde dispositivo: {DeviceLogMessage}",
                logDto.LogMessage);

            if (logDto.InternalDeviceTemperature.HasValue)
            {
                using (LogContext.PushProperty("DeviceInternalTemp", logDto.InternalDeviceTemperature.Value))
                {
                    _logger.Log(logLevel, "Log recibido desde dispositivo: {DeviceLogMessage}", logDto.LogMessage);
                }
            }
            else
            {
                _logger.Log(logLevel, "Log recibido desde dispositivo: {DeviceLogMessage}", logDto.LogMessage);
            }

            return NoContent();
        }
    }

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
            _logger.LogInformation("Iniciando procesamiento de datos ambientales.");
            var result = await _dataSubmissionService.SaveAmbientDataAsync(deviceContext, ambientDataDto);
            return result.IsSuccess ? NoContent() : BadRequest(result.ErrorMessage);
        }
    }



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
            _logger.LogInformation("Iniciando procesamiento de datos de captura. Archivo de imagen presente: {HasImageFile}", imageFile != null);

            if (string.IsNullOrEmpty(thermalDataJson))
            {
                _logger.LogWarning("El JSON de datos térmicos está ausente en la petición.");
                return BadRequest("Thermal data JSON is missing.");
            }

            ThermalDataDto? thermalDataDto;
            try
            {
                thermalDataDto = JsonSerializer.Deserialize<ThermalDataDto>(thermalDataJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (thermalDataDto == null)
                {
                    _logger.LogWarning("El JSON de datos térmicos no pudo ser deserializado a un objeto.");
                    return BadRequest("Failed to deserialize thermal data.");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Excepción al deserializar el JSON de datos térmicos.");
                return BadRequest("Invalid JSON format for thermal data.");
            }

            var result = await _dataSubmissionService.SaveCaptureDataAsync(
                deviceContext, thermalDataDto, thermalDataJson, imageFile, DateTime.UtcNow);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Datos de captura procesados y guardados exitosamente.");
                return Ok(new { message = "Thermal data received and saved successfully." });
            }

            _logger.LogError("Fallo al guardar los datos de captura: {ErrorMessage}", result.ErrorMessage);
            return StatusCode(500, new { message = "Failed to save thermal data.", error = result.ErrorMessage });
        }
    }

    private DeviceIdentityContext? GetDeviceIdentityFromClaims()
    {
        var deviceIdClaim = User.FindFirstValue("DeviceId");
        if (string.IsNullOrEmpty(deviceIdClaim) || !int.TryParse(deviceIdClaim, out int deviceId) || deviceId <= 0)
        {
            _logger.LogError("Claim 'DeviceId' no encontrado o es inválido en el token JWT.");
            return null;
        }

        int.TryParse(User.FindFirstValue("PlantId"), out int plantId);
        int.TryParse(User.FindFirstValue("CropId"), out int cropId);

        return new DeviceIdentityContext { DeviceId = deviceId, PlantId = plantId, CropId = cropId };
    }
}