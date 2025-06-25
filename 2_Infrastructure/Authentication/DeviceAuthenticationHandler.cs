using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ArandanoIRT.Web._1_Application.Services.Contracts;

namespace ArandanoIRT.Web._2_Infrastructure.Authentication;

public class DeviceAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "DeviceAuthScheme";
    public string TokenHeaderName { get; set; } = "Authorization";
    public string TokenPrefix { get; set; } = "Device"; // Ej: "Device <token>"
}

public class DeviceAuthenticationHandler : AuthenticationHandler<DeviceAuthenticationOptions>
{
    private readonly IDeviceService _deviceService;

    public DeviceAuthenticationHandler(
        IOptionsMonitor<DeviceAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IDeviceService deviceService) // Inyectar IDeviceService
        : base(options, logger, encoder, clock)
    {
        _deviceService = deviceService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Si no está el header de autorización, no podemos autenticar.
        if (!Request.Headers.ContainsKey(Options.TokenHeaderName))
        {
            Logger.LogDebug("Header de autorización no encontrado: {HeaderName}", Options.TokenHeaderName);
            return AuthenticateResult.NoResult(); // O Fail("Authorization header not found.") si quieres un 401 inmediato
        }

        // Parsear el header de autorización.
        if (!AuthenticationHeaderValue.TryParse(Request.Headers[Options.TokenHeaderName], out var headerValue))
        {
            Logger.LogWarning("No se pudo parsear el header de autorización.");
            return AuthenticateResult.Fail("Invalid Authorization header format.");
        }

        // Verificar el prefijo del esquema (ej. "Device").
        if (!Options.TokenPrefix.Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogDebug("Esquema de autorización no coincide. Esperado: {ExpectedScheme}, Obtenido: {ActualScheme}", Options.TokenPrefix, headerValue.Scheme);
            return AuthenticateResult.NoResult(); // No es nuestro esquema, dejar que otro handler lo intente si existe.
        }

        if (string.IsNullOrEmpty(headerValue.Parameter))
        {
            Logger.LogWarning("Token de dispositivo faltante en el header de autorización después del prefijo.");
            return AuthenticateResult.Fail("Missing device token.");
        }

        var accessToken = headerValue.Parameter;
        Logger.LogDebug("Intentando validar token de dispositivo (prefijo): {TokenPrefix}", accessToken.Substring(0, Math.Min(10, accessToken.Length)));

        // Usar IDeviceService para validar el token y obtener detalles del dispositivo.
        var validationResult = await _deviceService.ValidateTokenAndGetDeviceDetailsAsync(accessToken);

        if (validationResult.IsFailure)
        {
            Logger.LogWarning("Validación de token de dispositivo fallida: {ErrorMessage}", validationResult.ErrorMessage);
            return AuthenticateResult.Fail(validationResult.ErrorMessage);
        }

        var deviceDetails = validationResult.Value;

        if (deviceDetails.RequiresTokenRefresh)
        {
            // El contrato dice que si un token es válido, se permite el acceso.
            // La necesidad de refresco es una información que el dispositivo podría usar,
            // pero no debería impedir la autenticación si el token aún es válido.
            // Podríamos añadir un custom claim o un item en HttpContext si necesitamos pasar esta info.
            // Por ahora, solo logueamos.
            Logger.LogInformation("Token para DeviceId {DeviceId} requiere refresco, pero sigue siendo válido para esta solicitud.", deviceDetails.DeviceId);
            // Response.Headers.Append("X-Token-Refresh-Required", "true"); // Opcional: informar al cliente
        }

        // Crear los claims para el usuario autenticado (el dispositivo).
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, deviceDetails.DeviceId.ToString(), ClaimValueTypes.Integer32),
            new Claim("DeviceId", deviceDetails.DeviceId.ToString(), ClaimValueTypes.Integer32), // Custom claim para fácil acceso
            new Claim("PlantId", deviceDetails.PlantId.ToString(), ClaimValueTypes.Integer32),
            new Claim("CropId", deviceDetails.CropId.ToString(), ClaimValueTypes.Integer32),
            new Claim("DataCollectionTimeMinutes", deviceDetails.DataCollectionTimeMinutes.ToString(), ClaimValueTypes.Integer)
        };

        foreach (var role in deviceDetails.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name); // Scheme.Name es "DeviceAuthScheme"
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogInformation("Dispositivo DeviceId: {DeviceId} autenticado exitosamente con DeviceAuthScheme.", deviceDetails.DeviceId);
        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // Este método se llama si la autenticación falla y se necesita un "challenge".
        // Para una API, usualmente solo devolvemos un 401.
        // El comportamiento por defecto ya hace esto, pero puedes personalizarlo.
        Response.StatusCode = 401;
        Logger.LogDebug("DeviceAuthenticationHandler: Devolviendo 401 Unauthorized como challenge.");
        // Puedes añadir un header WWW-Authenticate si es apropiado.
        // Response.Headers.Append("WWW-Authenticate", $"Device realm=\"API\"");
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        // Este método se llama si la autenticación tiene éxito pero la autorización falla (403).
        Response.StatusCode = 403;
        Logger.LogDebug("DeviceAuthenticationHandler: Devolviendo 403 Forbidden.");
        return Task.CompletedTask;
    }
}