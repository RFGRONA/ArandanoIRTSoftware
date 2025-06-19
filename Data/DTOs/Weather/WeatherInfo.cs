using System.Text.Json.Serialization;

namespace ArandanoIRT.Web.Data.DTOs.Weather;

// Este DTO es para la respuesta de nuestro IWeatherService
public class WeatherInfo
{
    public float? TemperatureCelsius { get; set; }
    public int? HumidityPercentage { get; set; } // WeatherAPI devuelve int
    public bool? IsNight { get; set; } // Derivado de is_day
    public string? ConditionText { get; set; } // NUEVA PROPIEDAD para la descripción del clima
}

// DTOs internos para deserializar la respuesta de WeatherAPI.com
// Estos pueden ser clases privadas o internas dentro del servicio si solo se usan allí.
// O públicas si se reutilizan. Por ahora, públicas en el mismo archivo por simplicidad.

public class WeatherApiResponse
{
    [JsonPropertyName("current")]
    public CurrentWeather? Current { get; set; }

    [JsonPropertyName("error")]
    public WeatherApiError? Error { get; set; } // Para capturar errores de la API
}

public class CurrentWeather
{
    [JsonPropertyName("temp_c")]
    public float TempC { get; set; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; } // Es un entero en la respuesta de WeatherAPI

    [JsonPropertyName("is_day")]
    public int IsDay { get; set; } // 1 = Yes, 0 = No

    [JsonPropertyName("condition")] // NUEVO: Añadir el objeto condition
    public WeatherCondition? Condition { get; set; }
}

public class WeatherCondition // NUEVA CLASE para el objeto condition
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public int Code { get; set; }
}

public class WeatherApiError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}