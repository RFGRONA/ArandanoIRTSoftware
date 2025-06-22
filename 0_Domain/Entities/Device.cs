using System.ComponentModel.DataAnnotations.Schema;

namespace ArandanoIRT.Web._0_Domain.Entities;

[Table("device_data")]
public class Device
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PlantId { get; set; }
    public int? CropId { get; set; }
    public int StatusId { get; set; }
    public short DataCollectionTimeMinutes { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? MacAddress { get; set; }
    
    // Navigation Properties
    public virtual Plant? Plant { get; set; }
    public virtual ICollection<DeviceActivation> DeviceActivations { get; set; } = new List<DeviceActivation>();
    public virtual ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();
    public virtual ICollection<DeviceLog> DeviceLogs { get; set; } = new List<DeviceLog>();
    public virtual ICollection<EnvironmentalReading> EnvironmentalReadings { get; set; } = new List<EnvironmentalReading>();
    public virtual ICollection<ThermalCapture> ThermalCaptures { get; set; } = new List<ThermalCapture>();

}