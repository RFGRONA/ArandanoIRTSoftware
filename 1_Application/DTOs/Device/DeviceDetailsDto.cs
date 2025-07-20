using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._1_Application.DTOs.Device;

public class DeviceDetailsDto
{
    public int Id { get; set; }
    [Display(Name = "Nombre Dispositivo")] public string Name { get; set; } = string.Empty;
    [Display(Name = "Dirección MAC")] public string? MacAddress { get; set; }
    [Display(Name = "Descripción")] public string? Description { get; set; }
    [Display(Name = "Planta Asociada")] public string PlantName { get; set; } = string.Empty;
    [Display(Name = "Cultivo")] public string CropName { get; set; } = string.Empty;
    [Display(Name = "Ciudad del Cultivo")] public string CropCityName { get; set; } = string.Empty;

    [Display(Name = "Intervalo Recolección (min)")]
    public short DataCollectionTimeMinutes { get; set; }

    [Display(Name = "Estado Dispositivo")] public string DeviceStatusName { get; set; } = string.Empty;
    [Display(Name = "Fecha de Registro")] public DateTime RegisteredAt { get; set; }

    [Display(Name = "Última Actualización")]
    public DateTime UpdatedAt { get; set; }

    // Información de Activación
    public DeviceActivationDetailsDto? ActivationDevices { get; set; }

    public class DeviceActivationDetailsDto
    {
        [Display(Name = "Código de Activación")]
        public int? ActivationId { get; set; }

        [Display(Name = "Código de Activación")]
        public string? ActivationCode { get; set; }

        [Display(Name = "Estado de Activación")]
        public string? ActivationStatusName { get; set; }

        [Display(Name = "Código Expira En")] public DateTime? ActivationCodeExpiresAt { get; set; }

        [Display(Name = "Fecha de Activación Dispositivo")]
        public DateTime? DeviceActivatedAt { get; set; }
    }
}