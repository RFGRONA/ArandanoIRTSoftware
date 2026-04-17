namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

/// <summary>
///     DTO de respuesta a una solicitud de activación de dispositivo exitosa.
///     Proporciona los tokens JWT y la configuración inicial al dispositivo.
/// </summary>
public class DeviceActivationResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiration { get; set; } // UTC
    public int DataCollectionTime { get; set; }
}