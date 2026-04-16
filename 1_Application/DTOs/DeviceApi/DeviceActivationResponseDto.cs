namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

public class DeviceActivationResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiration { get; set; } // UTC
    public int DataCollectionTime { get; set; }
}