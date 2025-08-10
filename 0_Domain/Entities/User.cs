using Microsoft.AspNetCore.Identity;

namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
///     Stores web application users and their credentials. Now inherits from IdentityUser.
/// </summary>
public class User : IdentityUser<int>
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public AccountSettings AccountSettings { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public virtual ICollection<InvitationCode> InvitationCodes { get; set; } = new List<InvitationCode>();
    public virtual ICollection<Observation> Observations { get; set; } = new List<Observation>();
    public virtual ICollection<PlantStatusHistory> PlantStatusHistories { get; set; } = new List<PlantStatusHistory>();
}