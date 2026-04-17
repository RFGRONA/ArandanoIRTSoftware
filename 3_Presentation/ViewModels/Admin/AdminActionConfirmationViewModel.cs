using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._3_Presentation.ViewModels.Admin;

/// <summary>
///     ViewModel para el formulario de confirmación de acciones administrativas sensibles,
///     donde el administrador debe reingresar su contraseña para proceder.
/// </summary>
public class AdminActionConfirmationViewModel
{
    [Required] public int AdminToDeleteId { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria para confirmar esta acción.")]
    [DataType(DataType.Password)]
    [Display(Name = "Tu Contraseña Actual")]
    public string CurrentAdminPassword { get; set; }
}