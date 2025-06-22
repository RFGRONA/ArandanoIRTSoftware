using System.ComponentModel.DataAnnotations.Schema;

namespace ArandanoIRT.Web._0_Domain.Entities;

[Table("device_activation")]
public class DeviceActivation
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string ActivationCode { get; set; } = string.Empty;
    public int StatusId { get; set; } 
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    
    // Navigation Properties
    public virtual Device Device { get; set; }
}