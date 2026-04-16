using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Weather;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IWeatherService
{
    Task<Result<WeatherInfo>> GetCurrentWeatherAsync(string cityQuery);
}