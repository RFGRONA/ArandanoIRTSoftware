using ArandanoIRT.Web._0_Domain.Enums;

namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Almacena los códigos de activación de un solo uso generados para registrar nuevos dispositivos en la plataforma.
/// </summary>
public partial class DeviceActivation
{
    public int Id { get; set; }

    public int DeviceId { get; set; }

    public string ActivationCode { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public ActivationStatus Status { get; set; }

    public virtual Device Device { get; set; } = null!;
}