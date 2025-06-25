using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

public class DeviceLogEntryDto
{
    [Required(ErrorMessage = "El tipo de log es requerido.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "El tipo de log debe tener entre 3 y 50 caracteres.")]
    public string LogType { get; set; } = string.Empty; // INFO, WARNING, ERROR

    [Required(ErrorMessage = "El mensaje del log es requerido.")]
    public string LogMessage { get; set; } = string.Empty;

    public float? InternalDeviceTemperature { get; set; } // Nullable para permitir NaN o valores no enviados

    public float? InternalDeviceHumidity { get; set; }    // Nullable para permitir NaN o valores no enviados
}