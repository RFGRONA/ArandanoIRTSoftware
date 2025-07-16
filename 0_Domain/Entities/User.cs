namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
///     Stores web application users and their credentials.
/// </summary>
public class User
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsAdmin { get; set; }

    public int? CropId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public virtual Crop? Crop { get; set; }

    public virtual ICollection<Crop> Crops { get; set; } = new List<Crop>();

    public virtual ICollection<InvitationCode> InvitationCodes { get; set; } = new List<InvitationCode>();

    public virtual ICollection<Observation> Observations { get; set; } = new List<Observation>();

    public virtual ICollection<PlantStatusHistory> PlantStatusHistories { get; set; }
}