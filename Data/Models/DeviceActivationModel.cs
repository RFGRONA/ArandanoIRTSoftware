using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace ArandanoIRT.Web.Data.Models;

[Table("device_activation")]
public class DeviceActivationModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("device_id")]
    public int DeviceId { get; set; } // FK a DeviceDataModel

    [Column("activation_code")]
    public string ActivationCode { get; set; } = string.Empty;

    [Column("status_id")]
    public int StatusId { get; set; } // FK a StatusModel

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("activated_at")]
    public DateTime? ActivatedAt { get; set; } // Nullable
}