namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Stores manual observations made by an agronomist or expert user.
/// </summary>
public partial class Observation
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public int UserId { get; set; }

    public string Description { get; set; } = null!;

    public short? SubjectiveRating { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Plant Plant { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
