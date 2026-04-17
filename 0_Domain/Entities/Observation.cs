namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Almacena las observaciones manuales realizadas por un agrónomo o un usuario experto sobre una planta.
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