using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._3_Presentation.ViewModels.Admin;

public class AdminActionConfirmationViewModel
{
    [Required] public int AdminToDeleteId { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria para confirmar esta acción.")]
    [DataType(DataType.Password)]
    [Display(Name = "Tu Contraseña Actual")]
    public string CurrentAdminPassword { get; set; }
}