namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

public class CropMonitorViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public AnalysisReadinessViewModel AnalysisReadiness { get; set; } = new();

    public List<PlantMonitorViewModel> Plants { get; set; } = new();
}