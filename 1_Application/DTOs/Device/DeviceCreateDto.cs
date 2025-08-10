using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web._1_Application.DTOs.Device;

public class DeviceCreateDto : IDeviceFormData
{
    [Required(ErrorMessage = "El nombre del dispositivo es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Debe asociar el dispositivo a una planta.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID de la planta no es válido.")]
    public int PlantId { get; set; }

    [Required(ErrorMessage = "El intervalo de recolección es obligatorio.")]
    [Range(1, 1440, ErrorMessage = "El intervalo debe estar entre 1 y 1440 minutos.")]
    public short DataCollectionIntervalMinutes { get; set; } = 15;

    [RegularExpression("^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$", ErrorMessage = "El formato de la dirección MAC no es válido.")]
    public string? MacAddress { get; set; }

    // Para poblar los DropDownLists en la vista
    public IEnumerable<SelectListItem> AvailablePlants { get; set; } = new List<SelectListItem>();

}