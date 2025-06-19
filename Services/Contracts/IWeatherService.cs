// Para Result
using ArandanoIRT.Web.Common;
using ArandanoIRT.Web.Data.DTOs.Weather; // Para WeatherInfo

namespace ArandanoIRT.Web.Services.Contracts;

public interface IWeatherService
{
    Task<Result<WeatherInfo>> GetCurrentWeatherAsync(string cityQuery);
}