using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web.Data.DTOs.Admin;

public interface IDeviceFormData
{
    [Required(ErrorMessage = "El nombre del dispositivo es requerido.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    [Display(Name = "Nombre del Dispositivo")]
    string Name { get; set; }

    [Display(Name = "Descripción (Opcional)")]
    [DataType(DataType.MultilineText)]
    string? Description { get; set; }

    [Required(ErrorMessage = "Debe seleccionar una planta.")]
    [Display(Name = "Planta Asociada")]
    int PlantId { get; set; } // FK a PlantDataModel

    [Required(ErrorMessage = "El tiempo de recolección es requerido.")]
    [Range(1, 1440, ErrorMessage = "El tiempo debe estar entre 1 y 1440 minutos.")]
    [Display(Name = "Intervalo de Recolección (minutos)")]
    short DataCollectionTimeMinutes { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un estado.")]
    [Display(Name = "Estado del Dispositivo")]
    int StatusId { get; set; } // FK a StatusModel (ej: Activo, Inactivo)
}