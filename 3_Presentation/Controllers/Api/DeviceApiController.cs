using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
using ArandanoIRT.Web._1_Application.Services.Contracts;

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

    // --- Endpoints de Activación y Tokens ---

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
            _logger.LogInformation("Dispositivo DeviceId: {DeviceId} activado exitosamente.", requestDto.DeviceId);
            return Ok(result.Value);
        }

        _logger.LogWarning("Fallo en la activación para DeviceId: {DeviceId}. Error: {ErrorMessage}", requestDto.DeviceId, result.ErrorMessage);
        // Devolver un error genérico al cliente para no dar pistas sobre la lógica interna.
        return BadRequest("Código de activación inválido, expirado, o la MAC ya está en uso.");
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

    // --- Endpoint de Health Check ---

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        _logger.LogDebug("Health check solicitado.");
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        });
    }

    // --- Endpoints de Datos (Protegidos) ---

    [HttpPost("ambient-data")]
    [Authorize(Policy = "DeviceAuthenticated")]
    public async Task<IActionResult> SubmitAmbientData([FromBody] AmbientDataDto ambientDataDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Payload de datos ambientales inválido.");
        }

        var deviceContext = GetDeviceIdentityFromClaims();
        if (deviceContext == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Error procesando identidad del dispositivo.");
        }
        _logger.LogInformation("Received ambiental data submission from DeviceId: {DeviceId}", deviceContext.DeviceId);

        var result = await _dataSubmissionService.SaveAmbientDataAsync(deviceContext, ambientDataDto);

        return result.IsSuccess ? NoContent() : BadRequest(result.ErrorMessage);
    }

    [HttpPost("capture-data")]
    [Authorize(Policy = "DeviceAuthenticated")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubmitCaptureData(
        [FromForm(Name = "thermal")] string thermalDataJson,
        [FromForm(Name = "image")] IFormFile? imageFile)
    {
        var deviceContext = GetDeviceIdentityFromClaims();
        if (deviceContext == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Error procesando identidad del dispositivo.");
        }
        _logger.LogInformation("Received thermal data submission from DeviceId: {DeviceId}", deviceContext.DeviceId);

        if (string.IsNullOrEmpty(thermalDataJson))
        {
            return BadRequest("Thermal data JSON is missing.");
        }

        ThermalDataDto? thermalDataDto;
        try
        {
            // Deserialize the JSON string that came in the form data
            thermalDataDto = JsonSerializer.Deserialize<ThermalDataDto>(thermalDataJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (thermalDataDto == null)
            {
                return BadRequest("Failed to deserialize thermal data.");
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize thermalDataJson.");
            return BadRequest("Invalid JSON format for thermal data.");
        }

        // The service signature expects: DeviceContext, DTO, JSON string, Image File, and Server Time.

        if (imageFile == null)
        {
            _logger.LogWarning("No image file provided for DeviceId: {DeviceId}", deviceContext.DeviceId);
        }

        var result = await _dataSubmissionService.SaveCaptureDataAsync(
            deviceContext,
            thermalDataDto,
            thermalDataJson, // Pass the original JSON string for the stats field
            imageFile,
            DateTime.UtcNow);

        if (result.IsSuccess)
        {
            return Ok(new { message = "Thermal data received and saved successfully." });
        }

        return StatusCode(500, new { message = "Failed to save thermal data.", error = result.ErrorMessage });
    }

    // --- Método Helper ---

    private DeviceIdentityContext? GetDeviceIdentityFromClaims()
    {
        var deviceIdClaim = User.FindFirstValue("DeviceId");
        if (string.IsNullOrEmpty(deviceIdClaim) || !int.TryParse(deviceIdClaim, out int deviceId) || deviceId <= 0)
        {
            _logger.LogError("Claim 'DeviceId' no encontrado o inválido.");
            return null;
        }

        int.TryParse(User.FindFirstValue("PlantId"), out int plantId);
        int.TryParse(User.FindFirstValue("CropId"), out int cropId);

        return new DeviceIdentityContext { DeviceId = deviceId, PlantId = plantId, CropId = cropId };
    }
}