using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ArandanoIRT.Web._2_Infrastructure.Authentication;

/// <summary>
///     Define las opciones de configuración para el esquema de autenticación de dispositivos.
/// </summary>
public class DeviceAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    ///     El nombre por defecto para este esquema de autenticación.
    /// </summary>
    public const string DefaultScheme = "DeviceAuthScheme";

    /// <summary>
    ///     El nombre del encabezado HTTP donde se buscará el token (generalmente "Authorization").
    /// </summary>
    public string TokenHeaderName { get; set; } = "Authorization";

    /// <summary>
    ///     El prefijo que debe preceder al token en el encabezado (ej. "Device <token>").
    /// </summary>
    public string TokenPrefix { get; set; } = "Device";
}

/// <summary>
///     Handler de autenticación personalizado para ASP.NET Core, responsable de validar los tokens enviados por los
///     dispositivos.
/// </summary>
public class DeviceAuthenticationHandler : AuthenticationHandler<DeviceAuthenticationOptions>
{
    private readonly IDeviceService _deviceService;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="DeviceAuthenticationHandler" />.
    /// </summary>
    public DeviceAuthenticationHandler(
        IOptionsMonitor<DeviceAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IDeviceService deviceService)
        : base(options, logger, encoder, clock)
    {
        _deviceService = deviceService;
    }

    /// <summary>
    ///     Orquesta el proceso principal de autenticación para cada petición.
    ///     Extrae el token del encabezado, lo valida usando IDeviceService y, si es exitoso, crea una identidad
    ///     (ClaimsPrincipal) para el dispositivo.
    /// </summary>
    /// <returns>Un resultado de autenticación que indica éxito o fracaso.</returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(Options.TokenHeaderName))
        {
            Logger.LogDebug("Encabezado de autorización no encontrado: {HeaderName}", Options.TokenHeaderName);
            return AuthenticateResult.NoResult();
        }

        if (!AuthenticationHeaderValue.TryParse(Request.Headers[Options.TokenHeaderName], out var headerValue))
        {
            Logger.LogWarning("No se pudo parsear el encabezado de autorización.");
            return AuthenticateResult.Fail("Formato de encabezado de autorización inválido.");
        }

        if (!Options.TokenPrefix.Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogDebug(
                "El esquema de autorización no coincide. Esperado: {ExpectedScheme}, Obtenido: {ActualScheme}",
                Options.TokenPrefix, headerValue.Scheme);
            return AuthenticateResult.NoResult();
        }

        if (string.IsNullOrEmpty(headerValue.Parameter))
        {
            Logger.LogWarning("Token de dispositivo faltante en el encabezado de autorización.");
            return AuthenticateResult.Fail("Falta el token del dispositivo.");
        }

        var accessToken = headerValue.Parameter;
        Logger.LogDebug("Intentando validar token de dispositivo (prefijo): {TokenPrefix}",
            accessToken.Substring(0, Math.Min(10, accessToken.Length)));

        var validationResult = await _deviceService.ValidateTokenAndGetDeviceDetailsAsync(accessToken);

        if (validationResult.IsFailure)
        {
            Logger.LogWarning("Validación de token de dispositivo fallida: {ErrorMessage}",
                validationResult.ErrorMessage);
            return AuthenticateResult.Fail(validationResult.ErrorMessage);
        }

        var deviceDetails = validationResult.Value;

        if (deviceDetails.RequiresTokenRefresh)
            // El token sigue siendo válido para esta petición, pero está cerca de expirar.
            // Se registra el evento para monitoreo. Opcionalmente, se podría añadir un encabezado en la respuesta.
            Logger.LogInformation(
                "Token para DeviceId {DeviceId} requiere refresco, pero es válido para esta solicitud.",
                deviceDetails.DeviceId);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, deviceDetails.DeviceId.ToString(), ClaimValueTypes.Integer32),
            new("DeviceId", deviceDetails.DeviceId.ToString(), ClaimValueTypes.Integer32),
            new("PlantId", deviceDetails.PlantId.ToString(), ClaimValueTypes.Integer32),
            new("CropId", deviceDetails.CropId.ToString(), ClaimValueTypes.Integer32),
            new("DataCollectionTimeMinutes", deviceDetails.DataCollectionTimeMinutes.ToString(),
                ClaimValueTypes.Integer)
        };

        foreach (var role in deviceDetails.Roles) claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogInformation("Dispositivo DeviceId: {DeviceId} autenticado exitosamente.", deviceDetails.DeviceId);
        return AuthenticateResult.Success(ticket);
    }

    /// <summary>
    ///     Maneja el desafío de autenticación, que se invoca cuando la autenticación falla y se requiere una acción.
    ///     Para una API, esto típicamente significa devolver un código de estado 401 Unauthorized.
    /// </summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Logger.LogDebug("DeviceAuthenticationHandler: Devolviendo 401 Unauthorized como challenge.");
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Maneja el acceso prohibido, que se invoca cuando la autenticación es exitosa pero la autorización falla.
    ///     Esto resulta en un código de estado 403 Forbidden.
    /// </summary>
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        Logger.LogDebug("DeviceAuthenticationHandler: Devolviendo 403 Forbidden.");
        return Task.CompletedTask;
    }
}