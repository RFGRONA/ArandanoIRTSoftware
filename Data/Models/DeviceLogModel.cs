using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace ArandanoIRT.Web.Data.Models;

[Table("device_log")]
public class DeviceLogModel : BaseModel
{
    [PrimaryKey("id", false)] // false porque es BIGSERIAL
    public long Id { get; set; }

    [Column("device_id")]
    public int DeviceId { get; set; } // FK a DeviceDataModel

    [Column("log_type")]
    public string LogType { get; set; } = string.Empty;

    [Column("log_message")]
    public string LogMessage { get; set; } = string.Empty;

    [Column("log_timestamp_server")]
    public DateTime LogTimestampServer { get; set; }
    
    [Column("internal_device_temperature")]
    public float? InternalDeviceTemperature { get; set; } // Nullable

    [Column("internal_device_humidity")]
    public float? InternalDeviceHumidity { get; set; } // Nullable
}