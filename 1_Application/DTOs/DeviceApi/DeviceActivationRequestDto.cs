using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

/// <summary>
///     DTO para la petición de activación inicial que envía un dispositivo.
///     Contiene el ID, el código de activación y la dirección MAC para validarse.
/// </summary>
public class DeviceActivationRequestDto
{
    [Required] public int DeviceId { get; set; } = 0;

    [Required] public string ActivationCode { get; set; } = string.Empty;

    [Required] public string MacAddress { get; set; } = string.Empty;
}