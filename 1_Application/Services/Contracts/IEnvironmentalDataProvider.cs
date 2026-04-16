using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IEnvironmentalDataProvider
{
    /// <summary>
    ///     Gets consolidated and validated environmental data for analysis, including VPD.
    ///     It uses caching for weather data and implements a fallback to the light sensor if the API fails or conditions are
    ///     unsuitable.
    /// </summary>
    /// <param name="cityQuery">The city to query for weather (e.g., "Bogota").</param>
    /// <param name="lightIntensity">The current light intensity reading from the device sensor.</param>
    /// <param name="lightIntensityThreshold">The minimum light intensity required for the fallback logic.</param>
    /// <param name="ambientTemperatureC">The ambient temperature from the device sensor.</param>
    /// <param name="ambientHumidity">The ambient humidity from the device sensor.</param>
    /// <returns>A Result object containing the environmental data for analysis.</returns>
    Task<Result<EnvironmentalData>> GetEnvironmentalDataForAnalysisAsync(
        string cityQuery,
        double? lightIntensity,
        double lightIntensityThreshold,
        double ambientTemperatureC,
        double ambientHumidity);
}