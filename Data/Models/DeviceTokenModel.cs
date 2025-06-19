using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace ArandanoIRT.Web.Data.Models;

[Table("device_token")]
public class DeviceTokenModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("device_id")]
    public int DeviceId { get; set; } // FK a DeviceDataModel

    [Column("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [Column("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [Column("access_token_expires_at")]
    public DateTime AccessTokenExpiresAt { get; set; }

    [Column("refresh_token_expires_at")]
    public DateTime RefreshTokenExpiresAt { get; set; }

    [Column("status_id")]
    public int StatusId { get; set; } // FK a StatusModel

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; } // Nullable

    [Column("revoked_by_ip")]
    public string? RevokedByIp { get; set; } // Nullable
}