using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class ThermalCaptureSummaryDto
{
    public long Id { get; set; }
    [Display(Name = "Dispositivo")]
    public string DeviceName { get; set; } = "N/A";
    public int DeviceId { get; set; }
    [Display(Name = "Planta")]
    public string? PlantName { get; set; }
    [Display(Name = "Temp. Max (°C)")]
    public float? MaxTemp { get; set; }
    [Display(Name = "Temp. Min (°C)")]
    public float? MinTemp { get; set; }
    [Display(Name = "Temp. Prom. (°C)")]
    public float? AvgTemp { get; set; }
    [Display(Name = "Imagen RGB")]
    public string? RgbImagePath { get; set; } // URL a la imagen
    [Display(Name = "Registrado")]
    public DateTime RecordedAt { get; set; }
}