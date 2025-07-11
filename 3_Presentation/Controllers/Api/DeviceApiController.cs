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

        var result = await _dataSubmissionService.SaveAmbientDataAsync(deviceContext, ambientDataDto);

        return result.IsSuccess ? NoContent() : BadRequest(result.ErrorMessage);
    }

    [HttpPost("capture-data")]
    [Authorize(Policy = "DeviceAuthenticated")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubmitCaptureData(
        [FromForm(Name = "thermal")] string thermalDataJsonString,
        [FromForm(Name = "image")] IFormFile? imageFile)
    {
        if (string.IsNullOrWhiteSpace(thermalDataJsonString))
        {
            return BadRequest("Falta la parte 'thermal' en los datos.");
        }

        ThermalDataDto? thermalDataDto;
        try
        {
            thermalDataDto = JsonSerializer.Deserialize<ThermalDataDto>(thermalDataJsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (thermalDataDto == null) throw new JsonException("La deserialización resultó en null.");
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx, "Error al deserializar 'thermalDataJsonString': {JsonString}", thermalDataJsonString);
            return BadRequest("Formato JSON inválido para la parte 'thermal'.");
        }

        var deviceContext = GetDeviceIdentityFromClaims();
        if (deviceContext == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Error procesando identidad del dispositivo.");
        }
        
        var recordedAtServer = DateTime.UtcNow;
        var result = await _dataSubmissionService.SaveCaptureDataAsync(deviceContext, thermalDataDto, thermalDataJsonString, imageFile!, recordedAtServer);

        return result.IsSuccess ? NoContent() : BadRequest(result.ErrorMessage);
    }

    [HttpPost("log")]
    [Authorize(Policy = "DeviceAuthenticated")]
    public IActionResult SubmitDeviceLog([FromBody] DeviceLogEntryDto logEntryDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Payload de log inválido.");
        }

        var deviceContext = GetDeviceIdentityFromClaims();
        if (deviceContext == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Error procesando identidad del dispositivo.");
        }

        // Determinar el nivel de log
        var logLevel = logEntryDto.LogType?.ToUpper() switch
        {
            "WARNING" => LogLevel.Warning,
            "ERROR"   => LogLevel.Error,
            "FATAL"   => LogLevel.Critical,
            _         => LogLevel.Information // INFO y otros tipos por defecto a Information
        };

        // Crear un diccionario para los datos extra
        var extraData = new Dictionary<string, object?>();
        if(logEntryDto.InternalDeviceTemperature.HasValue) extraData["InternalDeviceTemperature"] = logEntryDto.InternalDeviceTemperature;
        if(logEntryDto.InternalDeviceHumidity.HasValue) extraData["InternalDeviceHumidity"] = logEntryDto.InternalDeviceHumidity;
        
        // Loguear usando el ILogger, que Serilog capturará. Incluir el DeviceId para el contexto.
        // Usamos un scope para enriquecer el log con datos adicionales si es necesario.
        using (_logger.BeginScope(extraData))
        {
            _logger.Log(logLevel, "Log desde DeviceId {DeviceId}: {Message}", deviceContext.DeviceId, logEntryDto.LogMessage);
        }

        return NoContent();
    }

    // --- Método Helper ---
    
    private DeviceIdentityContext? GetDeviceIdentityFromClaims()
    {
        var deviceIdClaim = User.FindFirstValue("DeviceId");
        if (!int.TryParse(deviceIdClaim, out int deviceId) || deviceId <= 0)
        {
            _logger.LogError("Claim 'DeviceId' no encontrado o inválido.");
            return null;
        }

        int.TryParse(User.FindFirstValue("PlantId"), out int plantId);
        int.TryParse(User.FindFirstValue("CropId"), out int cropId);

        return new DeviceIdentityContext { DeviceId = deviceId, PlantId = plantId, CropId = cropId };
    }
}