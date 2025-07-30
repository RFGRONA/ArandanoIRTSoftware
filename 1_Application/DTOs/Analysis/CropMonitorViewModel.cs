namespace ArandanoIRT.Web._1_Application.DTOs.Analysis;

public class CropMonitorViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsConfigurationValid { get; set; }
    public string ValidationMessage { get; set; }
    public List<PlantMonitorViewModel> Plants { get; set; } = new();
}