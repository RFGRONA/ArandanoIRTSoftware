using ArandanoIRT.Web._0_Domain.Enums;

namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

/// <summary>
///     ViewModel para representar una única planta en el dashboard de monitoreo.
/// </summary>
public class PlantMonitorViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public PlantStatus Status { get; set; }

    /// <summary>
    ///     Indica si la planta ya tiene una máscara térmica configurada.
    /// </summary>
    public bool HasMask { get; set; }

    public ExperimentalGroupType ExperimentalGroup { get; set; }
}