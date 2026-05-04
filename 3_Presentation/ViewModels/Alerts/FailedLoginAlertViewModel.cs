namespace ArandanoIRT.Web._3_Presentation.ViewModels.Alerts;

/// <summary>
///     ViewModel para la plantilla de correo electrónico que notifica sobre intentos de inicio de sesión fallidos.
/// </summary>
public class FailedLoginAlertViewModel
{
    public string? UserName { get; set; }
    public DateTime AlertTime { get; set; }
    public string? UserEmail { get; set; }
    public string? ForgotPasswordUrl { get; set; }
}