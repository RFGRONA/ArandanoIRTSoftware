using Microsoft.AspNetCore.Mvc;
// Para IDataSubmissionService y DeviceIdentityContext
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // Para [Authorize]
using System.Security.Claims; // Para ClaimTypes y User.FindFirstValue
using Microsoft.AspNetCore.Http; // Para IFormFile y StatusCodes
using System.Text.Json;
using ArandanoIRT.Web.Data.DTOs.DeviceApi;
using ArandanoIRT.Web.Services.Contracts; // Para JsonSerializer

namespace ArandanoIRT.Web.Controllers.Api;

[Route("api/device-api")]
[ApiController]
public class DeviceApiController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IDataSubmissionService _dataSubmissionService; // Añadir este servicio
    private readonly ILogger<DeviceApiController> _logger;

    public DeviceApiController(
        IDeviceService deviceService,
        IDataSubmissionService dataSubmissionService, // Inyectar IDataSubmissionService
        ILogger<DeviceApiController> logger)
    {
        _deviceService = deviceService;
        _dataSubmissionService = dataSubmissionService; // Asignar
        _logger = logger;
    }

    // Método helper para obtener el DeviceIdentityContext de los claims del usuario autenticado
    private DeviceIdentityContext? GetDeviceIdentityFromClaims()
    {
        var deviceIdClaim = User.FindFirstValue("DeviceId"); // Usar el custom claim "DeviceId"
        var plantIdClaim = User.FindFirstValue("PlantId");
        var cropIdClaim = User.FindFirstValue("CropId");

        if (string.IsNullOrEmpty(deviceIdClaim) || !int.TryParse(deviceIdClaim, out int deviceId) || deviceId <= 0)
        {
            _logger.LogError("Claim 'DeviceId' no encontrado o inválido para el dispositivo autenticado.");
            return null;
        }
        // PlantId y CropId pueden ser 0 o no estar presentes si el dispositivo no está asociado.
        // El servicio manejará valores 0 o nulos para estos si es necesario.
        int.TryParse(plantIdClaim, out int plantId); // plantId será 0 si no se puede parsear o no existe
        int.TryParse(cropIdClaim, out int cropId);   // cropId será 0 si no se puede parsear o no existe

        return new DeviceIdentityContext
        {
            DeviceId = deviceId,
            PlantId = plantId,
            CropId = cropId
        };
    }


    // POST /api/device-api/activate
    [HttpPost("activate")]
    public async Task<IActionResult> ActivateDevice([FromBody] DeviceActivationRequestDto requestDto)
    {
        if (!ModelState.IsValid)
        {
            // El logging de ModelState ya se hace en tu código original, lo cual es bueno.
            // _logger.LogWarning("Solicitud de activación inválida: {@ModelState}", ModelState);
            // Podríamos añadir el MAC al log de error de ModelState si está disponible.
            _logger.LogWarning("Solicitud de activación inválida para DeviceId: {DeviceId}, MAC: {MacAddress}. Errores: {@ModelStateValues}",
                requestDto.DeviceId, requestDto.MacAddress, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return BadRequest("Invalid request payload.");
        }

        // Logging mejorado para incluir MAC Address
        _logger.LogInformation(
            "Recibida solicitud de activación para DeviceId: {DeviceId}, MAC: {MacAddress}",
            requestDto.DeviceId, requestDto.MacAddress);
        var result = await _deviceService.ActivateDeviceAsync(requestDto);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Dispositivo DeviceId: {DeviceId}, MAC: {MacAddress} activado exitosamente.",
                requestDto.DeviceId, requestDto.MacAddress);
            return Ok(result.Value);
        }
        else
        {
            _logger.LogWarning(
                "Fallo en la activación para DeviceId: {DeviceId}, MAC: {MacAddress}. Error: {ErrorMessage}",
                requestDto.DeviceId, requestDto.MacAddress, result.ErrorMessage);
            if (result.ErrorMessage.Contains("expirado") || result.ErrorMessage.Contains("inválido") || result.ErrorMessage.Contains("no encontrado") || result.ErrorMessage.Contains("MAC"))
            {
                // Ampliamos un poco las condiciones para retornar un BadRequest más genérico en caso de error de MAC.
                return BadRequest("Invalid or expired activation code, or MAC address issue.");
            }
            return BadRequest(result.ErrorMessage); // O considera un StatusCode más específico si es apropiado
        }
    }

    // POST /api/device-api/auth
    [HttpPost("auth")]
    public async Task<IActionResult> Authenticate([FromBody] DeviceAuthRequestDto requestDto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Solicitud de autenticación inválida (auth): {@ModelState}", ModelState);
            return BadRequest("Authentication data cannot be null or invalid.");
        }
        _logger.LogInformation("Recibida solicitud de autenticación (auth) para token (prefijo): {TokenPrefix}", requestDto.Token.Substring(0, Math.Min(10, requestDto.Token.Length)));
        var result = await _deviceService.AuthenticateDeviceAsync(requestDto.Token);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Autenticación (auth) exitosa para token (prefijo): {TokenPrefix}", requestDto.Token.Substring(0, Math.Min(10, requestDto.Token.Length)));
            return Ok(result.Value);
        }
        else
        {
            _logger.LogWarning("Fallo en autenticación (auth) para token (prefijo): {TokenPrefix}. Error: {ErrorMessage}",
                requestDto.Token.Substring(0, Math.Min(10, requestDto.Token.Length)), result.ErrorMessage);
            if (result.ErrorMessage.Contains("Token inválido") || result.ErrorMessage.Contains("Token expirado"))
            {
                return Unauthorized("Invalid token.");
            }
            return BadRequest(result.ErrorMessage);
        }
    }

    // POST /api/device-api/refresh-token
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] DeviceAuthRequestDto requestDto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Solicitud de refresco de token inválida: {@ModelState}", ModelState);
            return BadRequest("Invalid request payload for refresh token.");
        }
        _logger.LogInformation("Recibida solicitud de refresco de token (prefijo de refresh token): {TokenPrefix}", requestDto.Token.Substring(0, Math.Min(10, requestDto.Token.Length)));
        var result = await _deviceService.RefreshDeviceTokenAsync(requestDto.Token);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Refresco de token exitoso para refresh token (prefijo): {TokenPrefix}", requestDto.Token.Substring(0, Math.Min(10, requestDto.Token.Length)));
            return Ok(result.Value);
        }
        else
        {
            _logger.LogWarning("Fallo en refresco de token para refresh token (prefijo): {TokenPrefix}. Error: {ErrorMessage}",
                requestDto.Token.Substring(0, Math.Min(10, requestDto.Token.Length)), result.ErrorMessage);
            if (result.ErrorMessage.Contains("Refresh token inválido") || result.ErrorMessage.Contains("Refresh token expirado"))
            {
                return Unauthorized("Invalid refresh token.");
            }
            return BadRequest(result.ErrorMessage);
        }
    }

    // --- Endpoints de Datos (Protegidos) ---

    // POST /api/device-api/ambient-data
    [HttpPost("ambient-data")]
    [Authorize(Policy = "DeviceAuthenticated")] // Aplicar la política de autorización
    public async Task<IActionResult> SubmitAmbientData([FromBody] AmbientDataDto ambientDataDto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Payload de datos ambientales inválido.");
            return BadRequest("Invalid ambient data payload."); // Contrato: HTTP 400 con mensaje
        }

        var deviceContext = GetDeviceIdentityFromClaims();
        if (deviceContext == null)
        {
            // Esto no debería suceder si [Authorize] funciona bien y el handler añade los claims.
             _logger.LogError("SubmitAmbientData: DeviceIdentityContext no pudo ser obtenido de los claims.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error procesando identidad del dispositivo.");
        }

        _logger.LogInformation("Recibida data ambiental del DeviceId: {DeviceId}. Light: {Light}, Temp: {Temp}, Hum: {Hum}",
            deviceContext.DeviceId, ambientDataDto.Light, ambientDataDto.Temperature, ambientDataDto.Humidity);

        var result = await _dataSubmissionService.SaveAmbientDataAsync(deviceContext, ambientDataDto);

        if (result.IsSuccess)
        {
            return NoContent(); // Contrato: HTTP 204 No Content
        }
        else
        {
            // Contrato: HTTP 400 Bad Request con mensaje de error (si es error de validación o proceso)
            // o HTTP 401 (ya manejado por [Authorize])
            _logger.LogError("Error al guardar datos ambientales para DeviceId {DeviceId}: {ErrorMessage}", deviceContext.DeviceId, result.ErrorMessage);
            return BadRequest(result.ErrorMessage);
        }
    }

    // POST /api/device-api/capture-data
    [HttpPost("capture-data")]
    [Authorize(Policy = "DeviceAuthenticated")]
    [Consumes("multipart/form-data")] // Indicar que este endpoint consume multipart
    public async Task<IActionResult> SubmitCaptureData(
        // ASP.NET Core enlaza las partes del multipart a los parámetros por nombre.
        // La especificación del payload multipart:
        // - `thermalData` (JSON): Parte con `Content-Type: application/json` y estructura `ThermalDataDto`.
        // - `imageFile` (File): Parte con `Content-Type: image/jpeg`
        // El firmware (MultipartDataSender.cpp) envía:
        // - Parte name="thermal", Content-Type: application/json (este es el JSON string)
        // - Parte name="image", filename="camera.jpg", Content-Type: image/jpeg (este es el IFormFile)
        // No envía "thermalImageDataJson" ni "recordedAt" como partes separadas.
        [FromForm(Name = "thermal")] string thermalDataJsonString, // El JSON como string crudo
        [FromForm(Name = "image")] IFormFile? imageFile // Nullable si la imagen es opcional
        )
    {
        // Validar thermalDataJsonString manualmente ya que es un string
        if (string.IsNullOrWhiteSpace(thermalDataJsonString))
        {
            _logger.LogWarning("La parte 'thermalDataJsonString' está vacía o ausente en la solicitud de captura.");
            return BadRequest("Missing or empty 'thermal' data part.");
        }

        ThermalDataDto? thermalDataDto;
        try
        {
            thermalDataDto = JsonSerializer.Deserialize<ThermalDataDto>(thermalDataJsonString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); // Para mapear max_temp a Max_Temp

            if (thermalDataDto == null) throw new JsonException("Deserialización de ThermalDataDto resultó en null.");

            // Validar el DTO deserializado explícitamente
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(thermalDataDto, serviceProvider: null, items: null);
            var validationResults = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool isValidDto = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(thermalDataDto, validationContext, validationResults, true);

            if (!isValidDto)
            {
                foreach (var validationResult in validationResults)
                {
                    _logger.LogWarning("Validación fallida para ThermalDataDto: {ErrorMessage}", validationResult.ErrorMessage);
                }
                // Podrías construir un mensaje de error más detallado si es necesario.
                return BadRequest("Invalid 'thermal' data content.");
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx, "Error al deserializar 'thermalDataJsonString': {JsonString}", thermalDataJsonString);
            return BadRequest("Invalid JSON format for 'thermal' data part.");
        }


        var deviceContext = GetDeviceIdentityFromClaims();
        if (deviceContext == null)
        {
             _logger.LogError("SubmitCaptureData: DeviceIdentityContext no pudo ser obtenido de los claims.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error procesando identidad del dispositivo.");
        }

        _logger.LogInformation("Recibida data de captura del DeviceId: {DeviceId}. Tamaño imagen: {ImageSize} bytes.",
            deviceContext.DeviceId, imageFile?.Length ?? 0);

        // Usar el tiempo del servidor para 'recordedAt'
        var recordedAtServer = DateTime.UtcNow;

        var result = await _dataSubmissionService.SaveCaptureDataAsync(
            deviceContext,
            thermalDataDto, // El DTO deserializado y validado
            thermalDataJsonString, // El string JSON original
            imageFile!, // Pasamos imageFile, puede ser null si el firmware no lo envía o es de noche y no se procesa
            recordedAtServer
        );

        if (result.IsSuccess)
        {
            return NoContent(); // Contrato: HTTP 204 No Content
        }
        else
        {
            _logger.LogError("Error al guardar datos de captura para DeviceId {DeviceId}: {ErrorMessage}", deviceContext.DeviceId, result.ErrorMessage);
            return BadRequest(result.ErrorMessage);
        }
    }


    // POST /api/device-api/log
    [HttpPost("log")]
    [Authorize(Policy = "DeviceAuthenticated")]
    public async Task<IActionResult> SubmitDeviceLog([FromBody] DeviceLogEntryDto logEntryDto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Payload de log de dispositivo inválido: {@ModelStateValues}",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return BadRequest("Invalid device log payload.");
        }

        var deviceContext = GetDeviceIdentityFromClaims();
        if (deviceContext == null)
        {
            _logger.LogError("SubmitDeviceLog: DeviceIdentityContext no pudo ser obtenido de los claims.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error procesando identidad del dispositivo.");
        }

        // Logging mejorado para incluir nuevos campos si están presentes
        _logger.LogInformation(
            "Recibido log del DeviceId: {DeviceId}. Tipo: {LogType}, Mensaje: {LogMessage}, TempInterna: {TempInt}, HumInterna: {HumInt}",
            deviceContext.DeviceId, logEntryDto.LogType, logEntryDto.LogMessage,
            logEntryDto.InternalDeviceTemperature?.ToString("F1") ?? "N/A", // Formato a 1 decimal o N/A
            logEntryDto.InternalDeviceHumidity?.ToString("F1") ?? "N/A");   // Formato a 1 decimal o N/A

        var result = await _dataSubmissionService.SaveDeviceLogAsync(deviceContext, logEntryDto);

        if (result.IsSuccess)
        {
            return NoContent();
        }
        else
        {
            _logger.LogError("Error al guardar log de dispositivo para DeviceId {DeviceId}: {ErrorMessage}", deviceContext.DeviceId, result.ErrorMessage);
            return BadRequest(result.ErrorMessage);
        }
    }
}