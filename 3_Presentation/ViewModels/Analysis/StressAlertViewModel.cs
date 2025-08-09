using ArandanoIRT.Web._0_Domain.Common;

namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

public class StressAlertViewModel
{
    public string UserName { get; set; }
    public string PlantName { get; set; }
    public string NewStatus { get; set; }
    public string PreviousStatus { get; set; }
    public float CwsiValue { get; set; }
    public string AlertTime { get; set; } = DateTime.UtcNow.ToColombiaTime().ToString("g");
    public string CtaButtonUrl { get; set; }
}