using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ArandanoIRT.Web._1_Application.Helper;

namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Contenedor principal para todas las configuraciones específicas de un cultivo.
/// </summary>
public class CropSettings
{
    [JsonPropertyName("analysis_parameters")]
    public AnalysisParameters AnalysisParameters { get; set; } = new();

    [JsonPropertyName("anomaly_parameters")]
    public AnomalyParameters AnomalyParameters { get; set; } = new();

    [JsonPropertyName("calibration_reminder")]
    public CalibrationReminder CalibrationReminder { get; set; } = new();
}

/// <summary>
/// Define los parámetros utilizados para el análisis de estrés hídrico.
/// </summary>
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

    [Required(ErrorMessage = "La temperatura mínima de canopia es obligatoria.")]
    [Range(5.0, 45.0, ErrorMessage = "El valor debe estar entre 5.0 y 45.0.")]
    [Display(Name = "Temperatura Mínima Válida de Canopia (°C)")]
    [JsonPropertyName("min_valid_canopy_temp")]
    public double MinValidCanopyTemp { get; set; } = 5.0;

    [Required(ErrorMessage = "La temperatura máxima de canopia es obligatoria.")]
    [Range(5.0, 45.0, ErrorMessage = "El valor debe estar entre 5.0 y 45.0.")]
    [Display(Name = "Temperatura Máxima Válida de Canopia (°C)")]
    [JsonPropertyName("max_valid_canopy_temp")]
    public double MaxValidCanopyTemp { get; set; } = 40.0;

    [Required(ErrorMessage = "La pendiente empírica M es obligatoria.")]
    [Display(Name = "Pendiente Empírica (M)")]
    [JsonPropertyName("empirical_m")]
    public double EmpiricalM { get; set; } = -1.56;

    [Required(ErrorMessage = "La intersección empírica C es obligatoria.")]
    [Display(Name = "Intersección Empírica (C)")]
    [JsonPropertyName("empirical_c")]
    public double EmpiricalC { get; set; } = -0.21;

    [Required(ErrorMessage = "El límite superior empírico es obligatorio.")]
    [Display(Name = "Límite Superior Empírico (UL)")]
    [JsonPropertyName("empirical_ul")]
    public double EmpiricalUl { get; set; } = 5.0;

    [Required(ErrorMessage = "La ventana de suavizado es obligatoria.")]
    [Range(10, 120, ErrorMessage = "La ventana debe estar entre 10 y 120 minutos.")]
    [Display(Name = "Ventana de Suavizado (min)")]
    [JsonPropertyName("smoothing_window_minutes")]
    public int SmoothingWindowMinutes { get; set; } = 60;
}

/// <summary>
/// Define los parámetros para la detección de anomalías en los datos.
/// </summary>
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

/// <summary>
/// Define los parámetros para los recordatorios de calibración de dispositivos.
/// </summary>
public class CalibrationReminder
{
    [Required(ErrorMessage = "El intervalo de recordatorio es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El intervalo debe ser de al menos 1 mes.")]
    [Display(Name = "Intervalo Recordatorio (meses)")]
    [JsonPropertyName("reminder_interval_months")]
    public int ReminderIntervalMonths { get; set; } = 3;
}