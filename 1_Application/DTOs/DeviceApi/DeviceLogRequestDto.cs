using System.Text.Json.Serialization;

namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

/// <summary>
/// Representa el payload que un dispositivo envía al endpoint de logs.
/// </summary>
public class DeviceLogRequestDto
{
    /// <summary>
    /// El tipo de log (ej. "INFO", "WARNING", "ERROR").
    /// </summary>
    [JsonPropertyName("logType")]
    public string LogType { get; set; } = string.Empty;

    /// <summary>
    /// El mensaje detallado del log.
    /// </summary>
    [JsonPropertyName("logMessage")]
    public string LogMessage { get; set; } = string.Empty;

    /// <summary>
    /// La temperatura interna opcional del dispositivo en el momento del log.
    /// </summary>
    [JsonPropertyName("internalDeviceTemperature")]
    public float? InternalDeviceTemperature { get; set; }

    /// <summary>
    /// El timestamp opcional del dispositivo en el momento del log.
    /// El firmware no lo envía actualmente, pero se añade para futura compatibilidad.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime? RecordedAtDevice { get; set; }
}