using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

public class AmbientDataDto
{
    // El firmware envía "light", "temperature", "humidity"
    // En nuestra tabla SensorData, "light" es lightIntensity
    // Mantendremos los nombres del DTO como los envía el dispositivo.

    [Required(ErrorMessage = "El nivel de luz es requerido.")]
    [Range(0, double.MaxValue, ErrorMessage = "El nivel de luz debe ser un valor positivo.")]
    public float Light { get; set; } // Coincide con "light" del firmware

    [Required(ErrorMessage = "La temperatura es requerida.")]
    public float Temperature { get; set; }

    [Required(ErrorMessage = "La humedad es requerida.")]
    [Range(0, 100, ErrorMessage = "La humedad debe estar entre 0 y 100.")]
    public float Humidity { get; set; }
    
    public DateTime? RecordedAtDevice { get; set; }
}