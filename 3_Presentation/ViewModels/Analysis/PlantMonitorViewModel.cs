using ArandanoIRT.Web._0_Domain.Enums;

namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

public class PlantMonitorViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public PlantStatus Status { get; set; }
    public bool HasMask { get; set; }
    public ExperimentalGroupType ExperimentalGroup { get; set; }
}