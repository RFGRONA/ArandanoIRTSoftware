using System.Text.Json;
using System.Web;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Weather;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

/// <summary>
///     Implementación del servicio que obtiene datos del clima.
///     Se comunica con la API externa (WeatherAPI.com) para obtener las condiciones climáticas actuales.
/// </summary>
public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private readonly WeatherApiSettings _settings;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="WeatherService" />.
    /// </summary>
    public WeatherService(
        IHttpClientFactory httpClientFactory,
        IOptions<WeatherApiSettings> settingsOptions,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("WeatherApi");
        _settings = settingsOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Esta implementación realiza una llamada HTTP GET a la API de WeatherAPI.com.
    ///     Incluye validaciones de configuración, manejo de errores de red y de la API,
    ///     y deserializa la respuesta JSON en los DTOs de la aplicación.
    /// </remarks>
    public async Task<Result<WeatherInfo>> GetCurrentWeatherAsync(string cityQuery)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("WeatherAPI Key no está configurada. No se puede obtener el clima.");
            return Result.Failure<WeatherInfo>("WeatherAPI Key no configurada.");
        }

        if (string.IsNullOrWhiteSpace(cityQuery))
        {
            _logger.LogWarning("Parámetro 'cityQuery' no proporcionado para WeatherAPI.");
            return Result.Failure<WeatherInfo>("Ciudad no especificada para la consulta del clima.");
        }

        var encodedCityQuery = HttpUtility.UrlEncode(cityQuery);
        // Se añade el parámetro lang=es para obtener las descripciones del clima en español.
        var requestUrl = $"current.json?key={_settings.ApiKey}&q={encodedCityQuery}&aqi=no&lang=es";

        _logger.LogInformation("Consultando WeatherAPI: {RequestUrl}", requestUrl);

        try
        {
            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                WeatherApiError? apiError = null;
                try
                {
                    apiError = JsonSerializer.Deserialize<WeatherApiError>(errorContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    /* ignorar error de deserialización */
                }

                var errorMessage = apiError?.Message != null && apiError.Code != 0
                    ? $"Error de WeatherAPI (Code: {apiError.Code}): {apiError.Message}"
                    : $"La petición a WeatherAPI falló con código {response.StatusCode}. Respuesta: {errorContent}";
                _logger.LogWarning(errorMessage);
                return Result.Failure<WeatherInfo>(errorMessage);
            }

            var weatherApiResponse = await response.Content.ReadFromJsonAsync<WeatherApiResponse>();

            if (weatherApiResponse?.Current?.Condition == null)
            {
                _logger.LogWarning("La respuesta de WeatherAPI no contenía datos válidos para {CityQuery}.", cityQuery);
                return Result.Failure<WeatherInfo>("Datos del clima no disponibles en la respuesta de la API.");
            }

            var current = weatherApiResponse.Current;
            var weatherInfo = new WeatherInfo
            {
                TemperatureCelsius = current.TempC,
                HumidityPercentage = current.Humidity,
                IsNight = current.IsDay == 0,
                ConditionText = current.Condition.Text
            };

            _logger.LogInformation(
                "Clima obtenido para {CityQuery}: Temp={TempC}, Hum={Humidity}%, EsNoche={IsNight}, Condición='{ConditionText}'",
                cityQuery, weatherInfo.TemperatureCelsius, weatherInfo.HumidityPercentage, weatherInfo.IsNight,
                weatherInfo.ConditionText);

            return Result.Success(weatherInfo);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de red al consultar WeatherAPI para {CityQuery}.", cityQuery);
            return Result.Failure<WeatherInfo>($"Error de red al contactar WeatherAPI: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error de deserialización al procesar respuesta de WeatherAPI para {CityQuery}.",
                cityQuery);
            return Result.Failure<WeatherInfo>($"Error al procesar la respuesta de WeatherAPI: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al consultar WeatherAPI para {CityQuery}.", cityQuery);
            return Result.Failure<WeatherInfo>($"Error interno al obtener datos del clima: {ex.Message}");
        }
    }
}