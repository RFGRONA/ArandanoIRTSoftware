using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Weather;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para un servicio que obtiene datos del clima de un proveedor externo.
/// </summary>
public interface IWeatherService
{
    /// <summary>
    ///     Obtiene las condiciones climáticas actuales para una ciudad específica.
    /// </summary>
    /// <param name="cityQuery">El nombre de la ciudad a consultar (ej. "Bogota").</param>
    /// <returns>Un objeto <c>Result</c> que contiene la información del clima (<c>WeatherInfo</c>) si la consulta es exitosa.</returns>
    Task<Result<WeatherInfo>> GetCurrentWeatherAsync(string cityQuery);
}