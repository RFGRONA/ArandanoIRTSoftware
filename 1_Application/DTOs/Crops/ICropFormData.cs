using System.ComponentModel.DataAnnotations;
using ArandanoIRT.Web._0_Domain.Entities;

namespace ArandanoIRT.Web._1_Application.DTOs.Crops;

public interface ICropFormData
{
    [Required(ErrorMessage = "El nombre del cultivo es requerido.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    [Display(Name = "Nombre del Cultivo")]
    public string Name { get; set; }

    [StringLength(200, ErrorMessage = "La dirección no debe exceder los 200 caracteres.")]
    [Display(Name = "Dirección (Opcional)")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "El nombre de la ciudad es requerido.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre de la ciudad debe tener entre 3 y 100 caracteres.")]
    [Display(Name = "Nombre de la Ciudad (para WeatherAPI)")]
    public string CityName { get; set; }
    
    [Display(Name = "Configuraciones del Cultivo")]
    public CropSettings CropSettings { get; set; }
}