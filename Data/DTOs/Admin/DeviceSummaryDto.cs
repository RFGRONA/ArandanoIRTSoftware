using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web.Data.DTOs.Admin;

public class DeviceSummaryDto
{
    public int Id { get; set; }
    [Display(Name = "Nombre Dispositivo")]
    public string Name { get; set; } = string.Empty;
    [Display(Name = "Planta Asociada")]
    public string PlantName { get; set; } = string.Empty; // Nombre de la planta
    [Display(Name = "Cultivo")]
    public string CropName { get; set; } = string.Empty; // Nombre del cultivo (vía planta)
    [Display(Name = "Estado Dispositivo")]
    public string DeviceStatusName { get; set; } = string.Empty;
    [Display(Name = "Estado Activación")]
    public string ActivationStatusName { get; set; } = "N/A"; // Ej: PENDIENTE_ACTIVATION, ACTIVE
    [Display(Name = "Fecha de Registro")]
    public DateTime RegisteredAt { get; set; }
}