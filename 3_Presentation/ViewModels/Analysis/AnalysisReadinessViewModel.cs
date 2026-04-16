namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

public class AnalysisReadinessViewModel
{
    public bool HasControlGroup { get; set; }
    public bool HasStressGroup { get; set; }
    public bool HasMonitoredGroup { get; set; }
    public bool HasControlWithMask { get; set; }
    public bool HasStressWithMask { get; set; }

    /// <summary>
    /// Determina si todas las condiciones estrictas necesarias para el análisis se cumplen.
    /// (Nota: La planta de Estrés térmico es opcional y por tanto, se excluye de la obligación principal)
    /// </summary>
    public bool IsReady => HasControlGroup && HasMonitoredGroup && HasControlWithMask;
}