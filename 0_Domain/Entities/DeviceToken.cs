using System.ComponentModel.DataAnnotations.Schema;

namespace ArandanoIRT.Web._0_Domain.Entities;

[Table("device_token")]
public class DeviceToken
{
    public int Id { get; set; }
    public int DeviceId { get; set; } 
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; } 
    public string? RevokedByIp { get; set; }
        
    // Navigation Properties
    public virtual Device Device { get; set; }
}