using ArandanoIRT.Web._0_Domain.Enums;

namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
///     Stores the history of status changes for each plant.
/// </summary>
public class PlantStatusHistory
{
    public long Id { get; set; }

    public int PlantId { get; set; }

    public PlantStatus Status { get; set; }

    public string? Observation { get; set; }

    public int? UserId { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual Plant Plant { get; set; } = null!;
    public virtual User? User { get; set; }
}