using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

public class DeviceAuthRequestDto
{
    [Required] public string Token { get; set; } = string.Empty;
}