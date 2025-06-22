using System.ComponentModel.DataAnnotations.Schema;

namespace ArandanoIRT.Web._0_Domain.Entities;

[Table("observations")]
public class Observation
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public int UserId { get; set; }
    public string Description { get; set; }
    public short? SubjectiveRating { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public virtual Plant Plant { get; set; }
    public virtual User User { get; set; }
}