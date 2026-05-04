namespace ArandanoIRT.Web._3_Presentation.ViewModels.Emails;

/// <summary>
///     ViewModel para la plantilla de correo que solicita una segunda firma (confirmación)
///     a otros administradores para autorizar la eliminación de una cuenta de administrador.
/// </summary>
public class AdminDeletionRequestViewModel
{
    public string RecipientName { get; set; }
    public string InitiatingAdminName { get; set; }
    public string AdminToDeleteName { get; set; }
    public string ConfirmationLink { get; set; }
}