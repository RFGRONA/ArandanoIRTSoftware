namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

public class CropMonitorViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsConfigurationValid { get; set; }
    public string ValidationMessage { get; set; }
    public List<PlantMonitorViewModel> Plants { get; set; } = new();
}