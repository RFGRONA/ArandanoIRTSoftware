using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para un servicio que provee datos ambientales consolidados y validados, listos para ser usados
///     en el análisis de estrés hídrico.
/// </summary>
public interface IEnvironmentalDataProvider
{
    /// <summary>
    ///     Obtiene los datos ambientales consolidados y validados para el análisis, incluyendo el VPD.
    ///     Utiliza un sistema de caché para los datos del clima y tiene una lógica de respaldo (fallback) que usa el sensor de
    ///     luz
    ///     si la API del clima falla o si las condiciones no son adecuadas.
    /// </summary>
    /// <param name="cityQuery">La ciudad para consultar el clima (ej. "Bogota").</param>
    /// <param name="lightIntensity">La lectura actual de intensidad lumínica del sensor del dispositivo.</param>
    /// <param name="lightIntensityThreshold">La intensidad lumínica mínima requerida para la lógica de respaldo.</param>
    /// <param name="ambientTemperatureC">La temperatura ambiente medida por el sensor del dispositivo.</param>
    /// <param name="ambientHumidity">La humedad ambiental medida por el sensor del dispositivo.</param>
    /// <returns>Un objeto <c>Result</c> que contiene los datos ambientales (<c>EnvironmentalData</c>) para el análisis.</returns>
    Task<Result<EnvironmentalData>> GetEnvironmentalDataForAnalysisAsync(
        string cityQuery,
        double? lightIntensity,
        double lightIntensityThreshold,
        double ambientTemperatureC,
        double ambientHumidity);
}