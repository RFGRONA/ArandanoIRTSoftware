using System.ComponentModel.DataAnnotations.Schema;

namespace ArandanoIRT.Web._0_Domain.Entities;

[Table("crop")]
public class Crop 
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string CityName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public virtual ICollection<Plant> Plants { get; set; } = new List<Plant>();
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}