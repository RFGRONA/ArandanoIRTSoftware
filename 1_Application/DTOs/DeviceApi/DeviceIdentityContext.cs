namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

/// <summary>
///     DTO interno para pasar el contexto de identidad de un dispositivo (sus IDs principales)
///     entre diferentes servicios de la aplicación.
/// </summary>
public class DeviceIdentityContext
{
    public int DeviceId { get; set; }
    public int PlantId { get; set; }
    public int CropId { get; set; }
}