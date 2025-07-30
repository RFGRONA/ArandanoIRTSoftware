using ArandanoIRT.Web._0_Domain.Common;

namespace ArandanoIRT.Web._1_Application.DTOs.Analysis;

public class AnomalyAlertViewModel
{
    public string UserName { get; set; }
    public string PlantName { get; set; }
    public int PlantId { get; set; }
    public string AlertTime { get; set; } = DateTime.UtcNow.ToColombiaTime().ToString("g");
}