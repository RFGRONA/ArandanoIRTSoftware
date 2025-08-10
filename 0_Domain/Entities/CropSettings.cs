using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ArandanoIRT.Web._0_Domain.Entities;

public class CropSettings
{
    [JsonPropertyName("analysis_parameters")]
    public AnalysisParameters AnalysisParameters { get; set; } = new();

    [JsonPropertyName("anomaly_parameters")]
    public AnomalyParameters AnomalyParameters { get; set; } = new();

    [JsonPropertyName("calibration_reminder")]
    public CalibrationReminder CalibrationReminder { get; set; } = new();
}

public class AnalysisParameters
{
    [Display(Name = "Umbral Estrés Incipiente")]
    [JsonPropertyName("cwsi_threshold_incipient")]
    public double CwsiThresholdIncipient { get; set; } = 0.3;

    [Display(Name = "Umbral Estrés Crítico")]
    [JsonPropertyName("cwsi_threshold_critical")]
    public double CwsiThresholdCritical { get; set; } = 0.5;

    [Display(Name = "Hora Inicio Análisis (24h)")]
    [JsonPropertyName("analysis_window_start_hour")]
    public int AnalysisWindowStartHour { get; set; } = 12;

    [Display(Name = "Hora Fin Análisis (24h)")]
    [JsonPropertyName("analysis_window_end_hour")]
    public int AnalysisWindowEndHour { get; set; } = 15;

    [Display(Name = "Umbral Intensidad Lumínica")]
    [JsonPropertyName("light_intensity_threshold")]
    public int LightIntensityThreshold { get; set; } = 600;
}

public class AnomalyParameters
{
    [Display(Name = "Umbral Delta T (°C)")]
    [JsonPropertyName("delta_t_threshold")]
    public double DeltaTThreshold { get; set; } = 1.5;

    [Display(Name = "Duración Mínima (minutos)")]
    [JsonPropertyName("duration_minutes")]
    public int DurationMinutes { get; set; } = 30;
}

public class CalibrationReminder
{
    [Display(Name = "Intervalo Recordatorio (meses)")]
    [JsonPropertyName("reminder_interval_months")]
    public int ReminderIntervalMonths { get; set; } = 3;
}