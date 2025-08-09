using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;
using ArandanoIRT.Web._1_Application.DTOs.Common;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
using ArandanoIRT.Web._1_Application.DTOs.SensorData;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public class DataQueryFilters // DTO para filtros comunes
{
    public int? DeviceId { get; set; }
    public int? PlantId { get; set; }
    public int? CropId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? LogLevel { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public interface IDataQueryService
{
    Task<Result<PagedResultDto<SensorDataDisplayDto>>> GetSensorDataAsync(DataQueryFilters filters);
    Task<Result<PagedResultDto<ThermalCaptureSummaryDto>>> GetThermalCapturesAsync(DataQueryFilters filters);
    Task<Result<ThermalCaptureDetailsDto?>> GetThermalCaptureDetailsAsync(long captureId);

    Task<Result<IEnumerable<SensorDataDisplayDto>>> GetAmbientDataForDashboardAsync(TimeSpan duration, int? cropId,
        int? plantId);

    Task<Result<ThermalStatsDto>> GetThermalStatsForDashboardAsync(TimeSpan duration, int? cropId, int? plantId);
    Task<Result<int>> GetActiveDevicesCountAsync(int? cropId, int? plantId);
    Task<Result<int>> GetMonitoredPlantsCountAsync(int? cropId);
    Task<Result<SensorDataDisplayDto?>> GetLatestAmbientDataAsync(int? cropId, int? plantId, int? deviceId);

    Task<Result<List<PlantRawDataDto>>> GetRawDataForAnalysisAsync(List<int> plantIds, DateTime startTime,
        DateTime endTime);

    Task<Result<(ThermalDataDto? Stats, string? ImagePath)>> GetLatestCaptureForMaskAsync(int plantId);
}