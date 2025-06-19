using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web.Data.DTOs.Admin;

public class ThermalCaptureDetailsDto
{
    public long Id { get; set; }
    [Display(Name = "Dispositivo")]
    public string DeviceName { get; set; } = "N/A";
    public int DeviceId { get; set; }
    [Display(Name = "Planta")]
    public string? PlantName { get; set; }
    [Display(Name = "Cultivo")]
    public string? CropName { get; set; }
    [Display(Name = "Temp. Max (°C)")]
    public float MaxTemp { get; set; }
    [Display(Name = "Temp. Min (°C)")]
    public float MinTemp { get; set; }
    [Display(Name = "Temp. Prom. (°C)")]
    public float AvgTemp { get; set; }
    [Display(Name = "Imagen RGB")]
    public string? RgbImagePath { get; set; }
    [Display(Name = "Registrado")]
    public DateTime RecordedAt { get; set; }

    // Para el heatmap
    public List<float?>? Temperatures { get; set; } // Array de temperaturas
    public int ThermalImageWidth { get; set; } = 32; // Asumimos 32x24, configurable si es necesario
    public int ThermalImageHeight { get; set; } = 24;
    public string? ThermalDataJson { get; set; } // El JSON crudo, por si el cliente quiere procesarlo de otra forma
}