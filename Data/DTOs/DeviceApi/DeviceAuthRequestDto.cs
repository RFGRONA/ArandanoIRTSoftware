using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web.Data.DTOs.DeviceApi;

public class DeviceAuthRequestDto 
{
    [Required] public string Token { get; set; } = string.Empty; 
}