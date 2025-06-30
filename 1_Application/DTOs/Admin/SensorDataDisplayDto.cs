using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class SensorDataDisplayDto
{
    public long Id { get; set; }
    [Display(Name = "Dispositivo")]
    public string DeviceName { get; set; } = "N/A";
    public int DeviceId { get; set; }

    [Display(Name = "Planta")]
    public string? PlantName { get; set; }

    [Display(Name = "Cultivo")]
    public string? CropName { get; set; }

    [Display(Name = "Luz")]
    public float? Light { get; set; }

    [Display(Name = "Temp. (°C)")]
    public float Temperature { get; set; }

    [Display(Name = "Humedad (%)")]
    public float Humidity { get; set; }

    [Display(Name = "Temp. Ciudad (°C)")]
    public float? CityTemperature { get; set; }

    [Display(Name = "Hum. Ciudad (%)")]
    public float? CityHumidity { get; set; }

    [Display(Name = "¿Noche?")]
    public bool? IsNight { get; set; }

    [Display(Name = "Condición Ciudad")]
    public string? CityWeatherCondition { get; set; }

    [Display(Name = "Registrado")]
    public DateTime RecordedAt { get; set; }
}