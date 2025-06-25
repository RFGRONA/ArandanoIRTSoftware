namespace ArandanoIRT.Web._1_Application.DTOs.DeviceApi;

public class AuthenticatedDeviceDetailsDto
{
    public int DeviceId { get; set; }
    public int PlantId { get; set; } // Necesario para asociar datos
    public int CropId { get; set; }  // Necesario para asociar datos y WeatherAPI
    public short DataCollectionTimeMinutes { get; set; }
    public List<string> Roles { get; set; } = new List<string> { "Device" }; // Rol fijo por ahora
    public bool RequiresTokenRefresh { get; set; } // Indica si el token está cerca de expirar
}