namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

/// <summary>
///     ViewModel que encapsula las comprobaciones de estado para determinar si un cultivo está correctamente configurado
///     para el análisis.
/// </summary>
public class AnalysisReadinessViewModel
{
    public bool HasControlGroup { get; set; }
    public bool HasStressGroup { get; set; }
    public bool HasMonitoredGroup { get; set; }
    public bool HasControlWithMask { get; set; }
    public bool HasStressWithMask { get; set; }

    /// <summary>
    ///     Determina si todas las condiciones necesarias para el análisis se cumplen.
    /// </summary>
    public bool IsReady => HasControlGroup && HasMonitoredGroup && HasControlWithMask;
}