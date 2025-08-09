using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

public class AmbientDataDto
{
    [JsonPropertyName("timestamp")]
    public DateTime? RecordedAtDevice { get; set; }
    
    [Required(ErrorMessage = "La temperatura es requerida.")]
    [JsonPropertyName("temperature")]
    public float Temperature { get; set; }

    [Required(ErrorMessage = "La humedad es requerida.")]
    [Range(0, 100, ErrorMessage = "La humedad debe estar entre 0 y 100.")]
    [JsonPropertyName("humidity")]
    public float Humidity { get; set; }
    
    [JsonPropertyName("light")]
    public float? Light { get; set; } 

    [JsonPropertyName("pressure")]
    public float? Pressure { get; set; }
}