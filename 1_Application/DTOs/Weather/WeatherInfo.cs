using System.Text.Json.Serialization;

namespace ArandanoIRT.Web._1_Application.DTOs.Weather;

/// <summary>
///     DTO principal que representa la información del clima de una forma limpia y estandarizada para ser usada dentro de
///     la aplicación.
/// </summary>
public class WeatherInfo
{
    public float? TemperatureCelsius { get; set; }
    public int? HumidityPercentage { get; set; }
    public bool? IsNight { get; set; }
    public string? ConditionText { get; set; }
}

/// <summary>
///     Modelo para deserializar la respuesta completa de la API externa WeatherAPI.com.
/// </summary>
public class WeatherApiResponse
{
    [JsonPropertyName("current")] public CurrentWeather? Current { get; set; }

    [JsonPropertyName("error")] public WeatherApiError? Error { get; set; }
}

/// <summary>
///     Modelo para deserializar el objeto "current" de la respuesta de WeatherAPI.
/// </summary>
public class CurrentWeather
{
    [JsonPropertyName("temp_c")] public float TempC { get; set; }

    [JsonPropertyName("humidity")] public int Humidity { get; set; }

    [JsonPropertyName("is_day")] public int IsDay { get; set; }

    [JsonPropertyName("condition")] public WeatherCondition? Condition { get; set; }
}

/// <summary>
///     Modelo para deserializar el objeto "condition" dentro de la respuesta de WeatherAPI.
/// </summary>
public class WeatherCondition
{
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;

    [JsonPropertyName("icon")] public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("code")] public int Code { get; set; }
}

/// <summary>
///     Modelo para deserializar el objeto de error en caso de que la respuesta de WeatherAPI falle.
/// </summary>
public class WeatherApiError
{
    [JsonPropertyName("code")] public int Code { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
}