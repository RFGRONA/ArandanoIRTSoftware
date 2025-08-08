using Microsoft.AspNetCore.Mvc.Rendering;

// Asegúrate de tener este using para List<>

namespace ArandanoIRT.Web._1_Application.DTOs.SensorData;

// Para los datos de gráficos de series temporales
public class TimeSeriesChartDataDto
{
    public List<string> Labels { get; set; } = new();
    public List<float?> Values { get; set; } = new();
    public string? DataSetLabel { get; set; }
    public string? BorderColor { get; set; }
    public string? BackgroundColor { get; set; }
    public float? Tension { get; set; }
    public int? PointRadius { get; set; }
    public int? PointHoverRadius { get; set; }
}

// Para las estadísticas térmicas
public class ThermalStatsDto
{
    public float? AverageMaxTemp24h { get; set; }
    public float? AverageMinTemp24h { get; set; }
    public float? AverageAvgTemp24h { get; set; }
    public float? LatestMaxTemp { get; set; }
    public float? LatestMinTemp { get; set; }
    public float? LatestAvgTemp { get; set; }
    public DateTime? LatestThermalReadingTimestamp { get; set; }
}

public class DashboardViewModel
{
    // Filtros
    public int? SelectedCropId { get; set; }
    public int? SelectedPlantId { get; set; }
    public int? SelectedDeviceId { get; set; }

    // Listas para filtros
    public List<SelectListItem> AvailableCrops { get; set; } = new();
    public List<SelectListItem> AvailablePlants { get; set; } = new();

    // Datos para gráficos
    public TimeSeriesChartDataDto? TemperatureChartData { get; set; }
    public TimeSeriesChartDataDto? HumidityChartData { get; set; }
    public TimeSeriesChartDataDto? LightChartData { get; set; }

    // Estadísticas Térmicas
    public ThermalStatsDto? ThermalStatistics { get; set; }

    // KPIs
    public int ActiveDevicesCount { get; set; }
    public int PlantsMonitoredCount { get; set; }

    // Datos ambientales (promedios y última lectura)
    public SensorDataDisplayDto? LatestAmbientData { get; set; }
    public float? AverageAmbientTemperature24h { get; set; }
    public float? MaxAmbientTemperature24h { get; set; }
    public float? MinAmbientTemperature24h { get; set; }
    public float? AverageAmbientHumidity24h { get; set; }
    public float? MaxAmbientHumidity24h { get; set; }
    public float? MinAmbientHumidity24h { get; set; }
    public float? AverageAmbientLight24h { get; set; }
    public float? MaxAmbientLight24h { get; set; }
    public float? MinAmbientLight24h { get; set; }

    public List<ThermalCaptureSummaryDto> RecentCaptures { get; set; } = new();
    // ======================================================================
}