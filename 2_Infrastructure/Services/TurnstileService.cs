using System.Text.Json;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;

namespace ArandanoIRT.Web._2_Infrastructure.Services;

public class TurnstileService : ITurnstileService
{
    private readonly HttpClient _httpClient;
    private readonly TurnstileSettings _settings;
    private readonly ILogger<TurnstileService> _logger;

    // Clase interna para deserializar la respuesta de Cloudflare
    private class TurnstileResponse
    {
        public bool Success { get; set; }
    }

    public TurnstileService(HttpClient httpClient, IOptions<TurnstileSettings> settings, ILogger<TurnstileService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://challenges.cloudflare.com/");
    }

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
            _logger.LogWarning("Circuit Breaker activado. El servicio de Turnstile no responde. Se está permitiendo el acceso temporalmente sin validación de captcha.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrió una excepción al validar el token de Turnstile.");
            return false;
        }
    }
}