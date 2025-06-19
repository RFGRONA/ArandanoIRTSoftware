using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web.Data.DTOs.Admin;

public interface IPlantFormData
{
    [Required(ErrorMessage = "El nombre de la planta es requerido.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    [Display(Name = "Nombre de la Planta")]
    string Name { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un cultivo.")]
    [Display(Name = "Cultivo Asociado")]
    int CropId { get; set; } // FK a CropModel

    [Required(ErrorMessage = "Debe seleccionar un estado.")]
    [Display(Name = "Estado")]
    int StatusId { get; set; } // FK a StatusModel
}