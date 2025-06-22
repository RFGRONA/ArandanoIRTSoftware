using System.ComponentModel.DataAnnotations.Schema;

namespace ArandanoIRT.Web._0_Domain.Entities;

[Table("device_log")]
public class DeviceLog 
{
    public long Id { get; set; }
    public int DeviceId { get; set; }
    public string LogType { get; set; } = string.Empty;
    public string LogMessage { get; set; } = string.Empty;
    public DateTime LogTimestampServer { get; set; }
    public float? InternalDeviceTemperature { get; set; }
    public float? InternalDeviceHumidity { get; set; }
    
    // Navigation Properties
    public virtual Device Device { get; set; }
}