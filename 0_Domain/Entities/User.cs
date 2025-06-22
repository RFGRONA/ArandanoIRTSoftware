using System.ComponentModel.DataAnnotations.Schema;

namespace ArandanoIRT.Web._0_Domain.Entities;

[Table("users")]
public class User
{
    public int Id { get; set; }
    public int CropId { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; } 
    public DateTime CreatedAt { get; set; }
    
    // Navigation Properties
    public virtual Crop Crop { get; set; }
    public virtual ICollection<Observation> Observations { get; set; } = new List<Observation>();
}