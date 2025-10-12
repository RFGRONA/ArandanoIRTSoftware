namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

/// <summary>
///     DTO que almacena los detalles de un dispositivo ya autenticado.
///     Esta información se utiliza internamente en el servidor para contextualizar las peticiones del dispositivo.
/// </summary>
public class AuthenticatedDeviceDetailsDto
{
    public int DeviceId { get; set; }
    public int PlantId { get; set; }
    public int CropId { get; set; }
    public short DataCollectionTimeMinutes { get; set; }
    public List<string> Roles { get; set; } = new() { "Device" };
    public bool RequiresTokenRefresh { get; set; }
}