using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

public class DeviceActivationRequestDto
{
    [Required]
    public int DeviceId { get; set; } = 0;

    [Required]
    public string ActivationCode { get; set; } = string.Empty;

    [Required]
    public string MacAddress { get; set; } = string.Empty;
}