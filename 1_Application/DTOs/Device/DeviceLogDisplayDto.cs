using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Device;

public class DeviceLogDisplayDto
{
    public long Id { get; set; }
    [Display(Name = "Dispositivo")]
    public string DeviceName { get; set; } = "N/A";
    public int DeviceId { get; set; }
    [Display(Name = "Tipo")]
    public string LogType { get; set; } = string.Empty;
    [Display(Name = "Mensaje")]
    public string LogMessage { get; set; } = string.Empty;
    [Display(Name = "Timestamp (Servidor)")]
    public DateTime LogTimestampServer { get; set; }

    [Display(Name = "Temp. (°C)")]
    public float? InternalDeviceTemperature { get; set; }

    [Display(Name = "Hum. (%)")]
    public float? InternalDeviceHumidity { get; set; }
}