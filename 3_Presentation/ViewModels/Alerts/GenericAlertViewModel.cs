namespace ArandanoIRT.Web._3_Presentation.ViewModels.Alerts;

/// <summary>
///     ViewModel para la plantilla de correo electrónico de una alerta genérica y personalizable.
/// </summary>
public class GenericAlertViewModel
{
    /// <summary>
    ///     El título principal de la alerta.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    ///     El mensaje o cuerpo detallado de la alerta.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    ///     La fecha y hora en que se generó la alerta.
    /// </summary>
    public DateTime? AlertTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     El nivel de severidad de la alerta (ej. "Precaución", "Crítico").
    /// </summary>
    public string Severity { get; set; }
}