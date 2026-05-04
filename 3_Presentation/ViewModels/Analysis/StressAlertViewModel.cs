using ArandanoIRT.Web._0_Domain.Common;

namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

/// <summary>
///     ViewModel para la plantilla de correo electrónico que notifica sobre un cambio en el estado de estrés hídrico.
/// </summary>
public class StressAlertViewModel
{
    public string UserName { get; set; }
    public string PlantName { get; set; }
    public string NewStatus { get; set; }
    public string PreviousStatus { get; set; }
    public float CwsiValue { get; set; }
    public string AlertTime { get; set; } = DateTime.UtcNow.ToColombiaTime().ToString("g");

    /// <summary>
    ///     URL del botón de llamada a la acción en el correo (dirige a la página de análisis).
    /// </summary>
    public string CtaButtonUrl { get; set; }
}