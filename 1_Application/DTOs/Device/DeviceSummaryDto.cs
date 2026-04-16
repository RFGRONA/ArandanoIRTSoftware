using System.ComponentModel.DataAnnotations;
using ArandanoIRT.Web._0_Domain.Enums;

namespace ArandanoIRT.Web._1_Application.DTOs.Device;

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
    public DeviceStatus DeviceStatus { get; set; }
    [Display(Name = "Estado Activación")]
    public ActivationStatus ActivationStatus { get; set; }
    [Display(Name = "Fecha de Registro")]
    public DateTime RegisteredAt { get; set; }
}