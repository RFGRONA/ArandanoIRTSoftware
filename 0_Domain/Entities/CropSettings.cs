using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ArandanoIRT.Web._1_Application.Helper;

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

[ValidateAnalysisParameters]
public class AnalysisParameters
{
    [Required(ErrorMessage = "El umbral de estrés incipiente es obligatorio.")]
    [Range(0.0, 1.0, ErrorMessage = "El valor debe estar entre 0.0 y 1.0.")]
    [Display(Name = "Umbral Estrés Incipiente")]
    [JsonPropertyName("cwsi_threshold_incipient")]
    public double CwsiThresholdIncipient { get; set; } = 0.3;

    [Required(ErrorMessage = "El umbral de estrés crítico es obligatorio.")]
    [Range(0.0, 1.0, ErrorMessage = "El valor debe estar entre 0.0 y 1.0.")]
    [Display(Name = "Umbral Estrés Crítico")]
    [JsonPropertyName("cwsi_threshold_critical")]
    public double CwsiThresholdCritical { get; set; } = 0.5;

    [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
    [Range(0, 23, ErrorMessage = "La hora debe estar entre 0 y 23.")]
    [Display(Name = "Hora Inicio Análisis (24h)")]
    [JsonPropertyName("analysis_window_start_hour")]
    public int AnalysisWindowStartHour { get; set; } = 12;

    [Required(ErrorMessage = "La hora de fin es obligatoria.")]
    [Range(0, 23, ErrorMessage = "La hora debe estar entre 0 y 23.")]
    [Display(Name = "Hora Fin Análisis (24h)")]
    [JsonPropertyName("analysis_window_end_hour")]
    public int AnalysisWindowEndHour { get; set; } = 15;

    [Required(ErrorMessage = "El umbral de luz es obligatorio.")]
    [Range(0, int.MaxValue, ErrorMessage = "El valor no puede ser negativo.")]
    [Display(Name = "Umbral Intensidad Lumínica")]
    [JsonPropertyName("light_intensity_threshold")]
    public int LightIntensityThreshold { get; set; } = 600;
}

public class AnomalyParameters
{
    [Required(ErrorMessage = "El umbral Delta T es obligatorio.")]
    [Display(Name = "Umbral Delta T (°C)")]
    [JsonPropertyName("delta_t_threshold")]
    public double DeltaTThreshold { get; set; } = 1.5;

    [Required(ErrorMessage = "La duración mínima es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La duración debe ser de al menos 1 minuto.")]
    [Display(Name = "Duración Mínima (minutos)")]
    [JsonPropertyName("duration_minutes")]
    public int DurationMinutes { get; set; } = 30;
}

public class CalibrationReminder
{
    [Required(ErrorMessage = "El intervalo de recordatorio es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El intervalo debe ser de al menos 1 mes.")]
    [Display(Name = "Intervalo Recordatorio (meses)")]
    [JsonPropertyName("reminder_interval_months")]
    public int ReminderIntervalMonths { get; set; } = 3;
}