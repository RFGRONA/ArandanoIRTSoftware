using System.Text.Json;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;

namespace ArandanoIRT.Web._2_Infrastructure.Services;

/// <summary>
/// Implementación del servicio que valida los tokens de Cloudflare Turnstile.
/// Se comunica con la API de Cloudflare para verificar la validez de un token de captcha.
/// </summary>
public class TurnstileService : ITurnstileService
{
    private readonly HttpClient _httpClient;
    private readonly TurnstileSettings _settings;
    private readonly ILogger<TurnstileService> _logger;

    /// <summary>
    /// DTO interno para deserializar la respuesta JSON de la API de Cloudflare.
    /// </summary>
    private class TurnstileResponse
    {
        public bool Success { get; set; }
    }

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="TurnstileService"/>.
    /// </summary>
    public TurnstileService(HttpClient httpClient, IOptions<TurnstileSettings> settings, ILogger<TurnstileService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://challenges.cloudflare.com/");
    }

    /// <inheritdoc />
    /// <remarks>
    /// Esta implementación realiza una petición POST al endpoint "siteverify" de Cloudflare.
    /// Incluye una lógica de "fail-open": si el servicio de Cloudflare no responde y el Circuit Breaker se activa,
    /// la validación devolverá 'true' para no bloquear a los usuarios legítimos.
    /// </remarks>
    public async Task<bool> IsTokenValid(string token)
    {
        _logger.LogInformation("Iniciando validación de token de Turnstile.");

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("El token de Turnstile está vacío o nulo. Validación fallida.");
            return false;
        }

        if (string.IsNullOrEmpty(_settings.SecretKey) || _settings.SecretKey == "TU_SECRET_KEY_AQUI")
        {
            _logger.LogError("La SecretKey de Turnstile no está configurada. Revisa tu appsettings.json.");
            return false;
        }

        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _settings.SecretKey),
                new KeyValuePair<string, string>("response", token)
            });

            _logger.LogInformation("Enviando petición de validación a Cloudflare.");
            var response = await _httpClient.PostAsync("turnstile/v0/siteverify", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Respuesta de Cloudflare: {Response}", responseString);

            var turnstileResponse = JsonSerializer.Deserialize<TurnstileResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var isValid = turnstileResponse?.Success ?? false;
            _logger.LogInformation("El token de Turnstile es {IsValid}", isValid ? "VÁLIDO" : "INVÁLIDO");

            return isValid;
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit Breaker activado. El servicio de Turnstile no responde. Se permite el acceso temporalmente sin validación.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrió una excepción al validar el token de Turnstile.");
            return false;
        }
    }
}