using ArandanoIRT.Web._1_Application.DTOs.Weather;
using ArandanoIRT.Web.Common;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IWeatherService
{
    Task<Result<WeatherInfo>> GetCurrentWeatherAsync(string cityQuery);
}