using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Web;
using ArandanoIRT.Web._1_Application.DTOs.Weather;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web.Common;
using ArandanoIRT.Web._2_Infrastructure.Settings;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly WeatherApiSettings _settings;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(
        IHttpClientFactory httpClientFactory,
        IOptions<WeatherApiSettings> settingsOptions,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("WeatherApi");
        _settings = settingsOptions.Value;
        _logger = logger;
    }

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

        string encodedCityQuery = HttpUtility.UrlEncode(cityQuery);
        // MODIFICADO: Añadido &lang=es
        var requestUrl = $"current.json?key={_settings.ApiKey}&q={encodedCityQuery}&aqi=no&lang=es";

        _logger.LogInformation("Consultando WeatherAPI: {RequestUrl}", requestUrl);

        try
        {
            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                WeatherApiError? apiError = null;
                try { apiError = JsonSerializer.Deserialize<WeatherApiError>(errorContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); } catch { /* ignorar */ }

                string errorMessage = apiError?.Message != null && apiError.Code != 0 ? // Comprobar si apiError tiene datos válidos
                    $"Error de WeatherAPI (Code: {apiError.Code}): {apiError.Message}" :
                    $"WeatherAPI request failed with status code {response.StatusCode}. Response: {errorContent}";
                _logger.LogWarning(errorMessage);
                return Result.Failure<WeatherInfo>(errorMessage);
            }

            // Usar JsonSerializerOptions si es necesario para PropertyNameCaseInsensitive globalmente,
            // aunque JsonPropertyName lo maneja bien.
            var weatherApiResponse = await response.Content.ReadFromJsonAsync<WeatherApiResponse>();

            if (weatherApiResponse?.Current == null || weatherApiResponse.Current.Condition == null) 
            {
                _logger.LogWarning("Respuesta de WeatherAPI no contenía datos 'current' o 'condition' válidos para {CityQuery}.", cityQuery);
                return Result.Failure<WeatherInfo>("Datos del clima (current o condition) no disponibles en la respuesta de la API.");
            }

            var current = weatherApiResponse.Current;
            var weatherInfo = new WeatherInfo
            {
                TemperatureCelsius = current.TempC,
                HumidityPercentage = current.Humidity,
                IsNight = (current.IsDay == 0),
                ConditionText = current.Condition.Text 
            };

            _logger.LogInformation("Clima obtenido para {CityQuery}: Temp={TempC}, Hum={Humidity}%, IsNight={IsNight}, Condición='{ConditionText}'",
                cityQuery, weatherInfo.TemperatureCelsius, weatherInfo.HumidityPercentage, weatherInfo.IsNight, weatherInfo.ConditionText);

            return Result.Success(weatherInfo);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HttpRequestException al consultar WeatherAPI para {CityQuery}.", cityQuery);
            return Result.Failure<WeatherInfo>($"Error de red al contactar WeatherAPI: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JsonException al deserializar respuesta de WeatherAPI para {CityQuery}.", cityQuery);
            return Result.Failure<WeatherInfo>($"Error al procesar la respuesta de WeatherAPI: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción inesperada al consultar WeatherAPI para {CityQuery}.", cityQuery);
            return Result.Failure<WeatherInfo>($"Error interno al obtener datos del clima: {ex.Message}");
        }
    }
}