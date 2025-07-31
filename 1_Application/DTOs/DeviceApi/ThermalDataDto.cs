using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

public class ThermalDataDto
{
    // El firmware envía "max_temp", "min_temp", "avg_temp", "temperatures"
    [Required(ErrorMessage = "La temperatura máxima es requerida.")]
    public float Max_Temp { get; set; } // El underscore es por el JSON del firmware

    [Required(ErrorMessage = "La temperatura mínima es requerida.")]
    public float Min_Temp { get; set; }

    [Required(ErrorMessage = "La temperatura promedio es requerida.")]
    public float Avg_Temp { get; set; }

    // El firmware envía null para NaN, por eso List<float?>
    // La especificación dice: temperatures (array de float?)
    // El firmware MultipartDataSender.cpp: tempArray.add(nullptr); // Represent NaN as null in JSON
    public List<float?>? Temperatures { get; set; } // Nullable si el array puede no venir o estar vacío

    // Validación para el tamaño del array si siempre se espera
    // Podríamos añadir una validación personalizada si es necesario, por ejemplo, que si no es null, debe tener 768 elementos.
    // [EnsureThermalArraySize(ErrorMessage = "El array de temperaturas debe contener 768 elementos.")]

    public DateTime? RecordedAtDevice { get; set; }

    public string? RgbImagePath { get; set; }
}