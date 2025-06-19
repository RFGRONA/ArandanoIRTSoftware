using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web.Data.DTOs.Admin;

// Para los datos de gráficos de series temporales
public class TimeSeriesChartDataDto 
{
    public List<string> Labels { get; set; } = new List<string>();
    public List<float?> Values { get; set; } = new List<float?>();
    public string? DataSetLabel { get; set; }
    
    // Propiedades añadidas para personalización del gráfico
    public string? BorderColor { get; set; }
    public string? BackgroundColor { get; set; }
    public float? Tension { get; set; }
    public int? PointRadius { get; set; }
    public int? PointHoverRadius { get; set; }
}

// Para las estadísticas térmicas
public class ThermalStatsDto
{
    // Promedios de las últimas 24h (o un periodo relevante)
    public float? AverageMaxTemp24h { get; set; }
    public float? AverageMinTemp24h { get; set; }
    public float? AverageAvgTemp24h { get; set; }

    // Última lectura
    public float? LatestMaxTemp { get; set; }
    public float? LatestMinTemp { get; set; }
    public float? LatestAvgTemp { get; set; }
    public DateTime? LatestThermalReadingTimestamp { get; set; }
}

public class DashboardViewModel
{
    // Filtros seleccionados
    public int? SelectedCropId { get; set; }
    public int? SelectedPlantId { get; set; }
    public int? SelectedDeviceId { get; set; } // Aunque el filtro principal sea por planta

    // Listas para los desplegables de filtro
    public List<SelectListItem> AvailableCrops { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> AvailablePlants { get; set; } = new List<SelectListItem>();

    // Datos para gráficos (últimas 24 horas)
    public TimeSeriesChartDataDto? TemperatureChartData { get; set; }
    public TimeSeriesChartDataDto? HumidityChartData { get; set; }
    public TimeSeriesChartDataDto? LightChartData { get; set; }

    // Estadísticas Térmicas
    public ThermalStatsDto? ThermalStatistics { get; set; }

    // Podríamos añadir otros KPIs, como número de dispositivos activos, alertas, etc.
    public int ActiveDevicesCount { get; set; }
    public int PlantsMonitoredCount { get; set; }
    
    // Datos ambientales
    public SensorDataDisplayDto? LatestAmbientData { get; set; }
    public float? AverageAmbientTemperature24h { get; set; }
    public float? MaxAmbientTemperature24h { get; set; } // NUEVO
    public float? MinAmbientTemperature24h { get; set; } // NUEVO

    public float? AverageAmbientHumidity24h { get; set; }
    public float? MaxAmbientHumidity24h { get; set; } // NUEVO
    public float? MinAmbientHumidity24h { get; set; } // NUEVO
    
    public float? AverageAmbientLight24h { get; set; }
    public float? MaxAmbientLight24h { get; set; } // NUEVO
    public float? MinAmbientLight24h { get; set; } // NUEVO
    
}
