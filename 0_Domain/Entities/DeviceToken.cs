using ArandanoIRT.Web._0_Domain.Enums;

namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Stores authentication tokens (JWTs) for devices.
/// </summary>
public partial class DeviceToken
{
    public int Id { get; set; }

    public int DeviceId { get; set; }

    public string AccessToken { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime AccessTokenExpiresAt { get; set; }

    public DateTime RefreshTokenExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }
    
    public TokenStatus Status { get; set; }

    public virtual Device Device { get; set; } = null!;
}
