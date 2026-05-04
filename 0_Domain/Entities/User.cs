using Microsoft.AspNetCore.Identity;

namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Representa a los usuarios de la aplicación web y sus credenciales.
/// Hereda de IdentityUser para integrarse con el sistema de autenticación de ASP.NET Core.
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