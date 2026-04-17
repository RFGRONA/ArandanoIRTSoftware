namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Almacena códigos de invitación de un solo uso para permitir el registro de nuevos usuarios en la plataforma.
/// </summary>
public partial class InvitationCode
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsAdmin { get; set; }

    public virtual User? CreatedByUser { get; set; }
}