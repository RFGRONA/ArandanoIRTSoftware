using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

public class ThermalDataDto
{
    [Required(ErrorMessage = "La temperatura máxima es requerida.")]
    public float Max_Temp { get; set; }

    [Required(ErrorMessage = "La temperatura mínima es requerida.")]
    public float Min_Temp { get; set; }

    [Required(ErrorMessage = "La temperatura promedio es requerida.")]
    public float Avg_Temp { get; set; }

    public List<float?>? Temperatures { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime? RecordedAtDevice { get; set; }

    public string? RgbImagePath { get; set; }
}