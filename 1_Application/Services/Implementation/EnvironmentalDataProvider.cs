using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;
using ArandanoIRT.Web._1_Application.DTOs.Weather;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

/// <summary>
///     Implementación del servicio que provee datos ambientales consolidados y validados.
///     Utiliza una caché para los datos del clima y un fallback al sensor de luz si la API externa falla o las condiciones
///     no son óptimas.
/// </summary>
public class EnvironmentalDataProvider : IEnvironmentalDataProvider
{
    /// <summary>
    ///     Define las palabras clave que indican condiciones climáticas adecuadas para un análisis válido.
    /// </summary>
    private static readonly string[] SuitableWeatherConditions = { "sunny", "clear", "despejado", "soleado" };

    private readonly ILogger<EnvironmentalDataProvider> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IWeatherService _weatherService;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="EnvironmentalDataProvider" />.
    /// </summary>
    public EnvironmentalDataProvider(
        IWeatherService weatherService,
        IMemoryCache memoryCache,
        ILogger<EnvironmentalDataProvider> logger)
    {
        _weatherService = weatherService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<EnvironmentalData>> GetEnvironmentalDataForAnalysisAsync(
        string cityQuery,
        double? lightIntensity,
        double lightIntensityThreshold,
        double ambientTemperatureC,
        double ambientHumidity)
    {
        var weatherResult = await GetWeatherWithCacheAsync(cityQuery);
        bool isConditionSuitable;

        if (weatherResult.IsSuccess && weatherResult.Value != null)
        {
            var conditionText = weatherResult.Value.ConditionText.ToLower();
            isConditionSuitable = SuitableWeatherConditions.Any(c => conditionText.Contains(c));

            if (!isConditionSuitable)
            {
                _logger.LogInformation(
                    "La condición climática '{Condition}' no es adecuada. Verificando fallback con sensor de luz.",
                    conditionText);
                isConditionSuitable = CheckLightSensorFallback(lightIntensity, lightIntensityThreshold);
            }
        }
        else
        {
            _logger.LogWarning("La API del clima falló. Utilizando fallback con sensor de luz.");
            isConditionSuitable = CheckLightSensorFallback(lightIntensity, lightIntensityThreshold);
        }

        var vpd = CalculateVpd(ambientTemperatureC, ambientHumidity);

        var environmentalData = new EnvironmentalData
        {
            IsConditionSuitable = isConditionSuitable,
            VpdKpa = vpd,
            AmbientTemperatureC = ambientTemperatureC,
            AmbientHumidity = ambientHumidity
        };

        return Result.Success(environmentalData);
    }

    /// <summary>
    ///     Obtiene la información del clima para una ciudad, priorizando la obtención de datos desde la caché.
    ///     Si no hay datos en caché, llama al servicio de clima y guarda el resultado por 30 minutos.
    /// </summary>
    /// <param name="cityQuery">La ciudad a consultar.</param>
    /// <returns>Un objeto Result con la información del clima.</returns>
    private async Task<Result<WeatherInfo>> GetWeatherWithCacheAsync(string cityQuery)
    {
        var cacheKey = $"weather_{cityQuery.ToLower().Replace(" ", "_")}";

        if (_memoryCache.TryGetValue(cacheKey, out Result<WeatherInfo>? cachedResult) && cachedResult != null)
        {
            _logger.LogInformation("Datos del clima para '{City}' encontrados en caché.", cityQuery);
            return cachedResult;
        }

        var weatherResult = await _weatherService.GetCurrentWeatherAsync(cityQuery);

        if (weatherResult.IsSuccess)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
            _memoryCache.Set(cacheKey, weatherResult, cacheEntryOptions);
        }

        return weatherResult;
    }

    /// <summary>
    ///     Lógica de respaldo que determina si las condiciones son adecuadas para el análisis basándose únicamente en el
    ///     sensor de luz.
    /// </summary>
    /// <param name="lightIntensity">La intensidad lumínica actual.</param>
    /// <param name="lightIntensityThreshold">El umbral mínimo de intensidad requerido.</param>
    /// <returns>True si la intensidad lumínica es suficiente, de lo contrario False.</returns>
    private bool CheckLightSensorFallback(double? lightIntensity, double lightIntensityThreshold)
    {
        if (!lightIntensity.HasValue)
        {
            _logger.LogWarning("Fallback con sensor de luz falló: No hay valor de intensidad disponible.");
            return false;
        }

        var isBrightEnough = lightIntensity.Value >= lightIntensityThreshold;
        if (isBrightEnough)
            _logger.LogInformation("Fallback con sensor de luz exitoso: Intensidad {Intensity} >= Umbral {Threshold}.",
                lightIntensity.Value, lightIntensityThreshold);
        else
            _logger.LogWarning("Fallback con sensor de luz falló: Intensidad {Intensity} < Umbral {Threshold}.",
                lightIntensity.Value, lightIntensityThreshold);
        return isBrightEnough;
    }

    /// <summary>
    ///     Calcula el Déficit de Presión de Vapor (VPD) en kPa usando la fórmula de Magnus-Tetens.
    /// </summary>
    /// <param name="temperatureC">Temperatura ambiente en Celsius.</param>
    /// <param name="relativeHumidity">Humedad relativa en porcentaje (ej. 65.0 para 65%).</param>
    /// <returns>VPD en kilopascales (kPa).</returns>
    private double CalculateVpd(double temperatureC, double relativeHumidity)
    {
        var svp = 0.6108 * Math.Exp(17.27 * temperatureC / (temperatureC + 237.3));
        var avp = svp * (relativeHumidity / 100.0);
        var vpd = svp - avp;

        return vpd;
    }
}