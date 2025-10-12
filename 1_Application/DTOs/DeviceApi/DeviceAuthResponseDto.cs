namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

/// <summary>
///     DTO de respuesta a una solicitud de refresco de token exitosa.
///     Proporciona un nuevo juego de tokens JWT al dispositivo.
/// </summary>
public class DeviceAuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiration { get; set; }
    public int DataCollectionTime { get; set; }
}