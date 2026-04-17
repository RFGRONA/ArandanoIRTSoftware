using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

/// <summary>
///     DTO para la petición de refresco de token de autenticación.
///     El dispositivo envía su Refresh Token para obtener un nuevo Access Token.
/// </summary>
public class DeviceAuthRequestDto
{
    [Required] public string Token { get; set; } = string.Empty;
}