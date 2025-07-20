using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._3_Presentation.ViewModels;
using ArandanoIRT.Web.Common;

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

    // Para el Dashboard
    Task<Result<IEnumerable<SensorDataDisplayDto>>> GetAmbientDataForDashboardAsync(TimeSpan duration, int? cropId,
        int? plantId, int? deviceId);

    Task<Result<ThermalStatsDto>> GetThermalStatsForDashboardAsync(TimeSpan duration, int? cropId, int? plantId,
        int? deviceId);

    Task<Result<int>> GetActiveDevicesCountAsync(int? cropId, int? plantId); // Para KPIs
    Task<Result<int>> GetMonitoredPlantsCountAsync(int? cropId); // Para KPIs
    Task<Result<SensorDataDisplayDto?>> GetLatestAmbientDataAsync(int? cropId, int? plantId, int? deviceId);
}