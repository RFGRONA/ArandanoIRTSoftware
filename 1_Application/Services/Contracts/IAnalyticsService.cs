using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IAnalyticsService
{
    Task<Result> SaveThermalMaskAsync(int plantId, string maskCoordinatesJson);
    Task<Result<List<CropMonitorViewModel>>> GetCropsForMonitoringAsync();
    Task<Result<AnalysisDetailsViewModel>> GetAnalysisDetailsAsync(int plantId, DateTime? startDate, DateTime? endDate);
}