using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;

namespace ArandanoIRT.Web._1_Application.DTOs.Reports;

public class PlantReportModel
{
    public string PlantName { get; set; }
    public string CropName { get; set; }
    public string DateRange { get; set; }
    public string GenerationDate { get; set; } = DateTime.Now.ToColombiaTime().ToString("dd/MM/yyyy HH:mm");

    // Resumen Ejecutivo
    public float? AverageCwsi { get; set; }
    public float? MaxCwsi { get; set; }
    public int MildStressAlerts { get; set; }
    public int SevereStressAlerts { get; set; }
    public int AnomalyAlerts { get; set; }

    // Datos para Gráficos y Tablas
    public List<AnalysisResultDataPoint> AnalysisData { get; set; } = new();
    public List<ObservationDataPoint> ObservationData { get; set; } = new();
    public List<PlantStatusHistory> StatusHistory { get; set; } = new();
}

public class AnalysisResultDataPoint
{
    public DateTime Timestamp { get; set; }
    public float CwsiValue { get; set; }
    public float CanopyTemperature { get; set; }
    public float AmbientTemperature { get; set; }
}

public class ObservationDataPoint
{
    public DateTime Timestamp { get; set; }
    public string UserName { get; set; }
    public string Description { get; set; }
}