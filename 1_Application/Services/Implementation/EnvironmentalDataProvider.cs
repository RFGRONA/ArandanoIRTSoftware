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
            // Check if weather condition text is suitable
            var conditionText = weatherResult.Value.ConditionText.ToLower();
            isConditionSuitable = SuitableWeatherConditions.Any(c => conditionText.Contains(c));

            if (!isConditionSuitable)
            {
                _logger.LogInformation(
                    "Weather condition '{Condition}' is not suitable. Checking light sensor fallback.", conditionText);
                isConditionSuitable = CheckLightSensorFallback(lightIntensity, lightIntensityThreshold);
            }
        }
        else
        {
            // Weather API failed, rely entirely on the light sensor
            _logger.LogWarning("Weather API failed or returned no data. Relying on light sensor fallback.");
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
            _logger.LogInformation("Weather data for '{City}' found in cache.", cityQuery);
            return cachedResult;
        }

        var weatherResult = await _weatherService.GetCurrentWeatherAsync(cityQuery);

        if (weatherResult.IsSuccess)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30)); // Cache for 30 minutes
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
            _logger.LogWarning("Light sensor fallback failed: No light intensity value available.");
            return false;
        }

        var isBrightEnough = lightIntensity.Value >= lightIntensityThreshold;
        if (isBrightEnough)
            _logger.LogInformation("Light sensor fallback successful: Intensity {Intensity} >= Threshold {Threshold}.",
                lightIntensity.Value, lightIntensityThreshold);
        else
            _logger.LogWarning("Light sensor fallback failed: Intensity {Intensity} < Threshold {Threshold}.",
                lightIntensity.Value, lightIntensityThreshold);
        return isBrightEnough;
    }

    /// <summary>
    ///     Calculates Vapor Pressure Deficit (VPD) in kPa using the Magnus-Tetens formula.
    /// </summary>
    /// <param name="temperatureC">Ambient temperature in Celsius.</param>
    /// <param name="relativeHumidity">Relative humidity as a percentage (e.g., 65.0 for 65%).</param>
    /// <returns>VPD in kilopascals (kPa).</returns>
    private double CalculateVpd(double temperatureC, double relativeHumidity)
    {
        // Magnus-Tetens formula to calculate Saturated Vapor Pressure (SVP)
        // SVP(T) = 0.6108 * exp((17.27 * T) / (T + 237.3))
        var svp = 0.6108 * Math.Exp(17.27 * temperatureC / (temperatureC + 237.3));

        // Actual Vapor Pressure (AVP)
        var avp = svp * (relativeHumidity / 100.0);

        // Vapor Pressure Deficit (VPD)
        var vpd = svp - avp;

        return vpd;
    }
}