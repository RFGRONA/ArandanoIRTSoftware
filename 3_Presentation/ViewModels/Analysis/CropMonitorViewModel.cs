namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

/// <summary>
///     ViewModel para representar un cultivo en el dashboard de monitoreo principal.
/// </summary>
public class CropMonitorViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }

    /// <summary>
    ///     Contiene el estado de preparación del cultivo para el análisis.
    /// </summary>
    public AnalysisReadinessViewModel AnalysisReadiness { get; set; } = new();

    /// <summary>
    ///     Lista de las plantas que pertenecen a este cultivo.
    /// </summary>
    public List<PlantMonitorViewModel> Plants { get; set; } = new();
}