using ArandanoIRT.Web._0_Domain.Common;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IAnalyticsService
{
    Task<Result> SaveThermalMaskAsync(int plantId, string maskCoordinatesJson);
}