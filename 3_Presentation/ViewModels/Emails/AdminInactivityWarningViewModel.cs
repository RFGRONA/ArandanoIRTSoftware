namespace ArandanoIRT.Web._3_Presentation.ViewModels.Emails;

/// <summary>
///     ViewModel para la plantilla de correo que envía advertencias por inactividad a las cuentas de administrador.
///     El contenido del correo varía según la cantidad de días de inactividad.
/// </summary>
public class AdminInactivityWarningViewModel
{
    public string UserName { get; set; }
    public int DaysInactive { get; set; }
    public string WarningTitle { get; set; }
    public string WarningMessage { get; set; }
    public string LoginUrl { get; set; }
}