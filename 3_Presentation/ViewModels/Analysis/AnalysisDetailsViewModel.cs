namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

public class AnalysisDetailsViewModel
{
    public int PlantId { get; set; }
    public string PlantName { get; set; }
    public string CropName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Datos pre-formateados como strings JSON para Chart.js
    public string CwsiChartDataJson { get; set; }
    public string TempChartDataJson { get; set; }
    public float CwsiThresholdIncipient { get; set; }
    public float CwsiThresholdCritical { get; set; }
}