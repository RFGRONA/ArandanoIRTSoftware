namespace ArandanoIRT.Web.Data.DTOs.DeviceApi;

public class DeviceAuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiration { get; set; } // UTC
    public int DataCollectionTime { get; set; }
}