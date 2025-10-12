using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

/// <summary>
///     DTO para recibir los datos de la cámara térmica enviados por un dispositivo.
///     Incluye estadísticas clave y, opcionalmente, la matriz completa de temperaturas.
/// </summary>
public class ThermalDataDto
{
    [Required(ErrorMessage = "La temperatura máxima es requerida.")]
    public float Max_Temp { get; set; }

    [Required(ErrorMessage = "La temperatura mínima es requerida.")]
    public float Min_Temp { get; set; }

    [Required(ErrorMessage = "La temperatura promedio es requerida.")]
    public float Avg_Temp { get; set; }

    public List<float?>? Temperatures { get; set; }

    [JsonPropertyName("timestamp")] public DateTime? RecordedAtDevice { get; set; }

    public string? RgbImagePath { get; set; }
}