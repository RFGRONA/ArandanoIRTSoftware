using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

/// <summary>
///     Representa una entrada de log de dispositivo de forma estructurada.
/// </summary>
public class DeviceLogEntryDto
{
    [Required(ErrorMessage = "El tipo de log es requerido.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "El tipo de log debe tener entre 3 y 50 caracteres.")]
    public string LogType { get; set; } = string.Empty;

    [Required(ErrorMessage = "El mensaje del log es requerido.")]
    public string LogMessage { get; set; } = string.Empty;

    public float? InternalDeviceTemperature { get; set; }

    public float? InternalDeviceHumidity { get; set; }
}