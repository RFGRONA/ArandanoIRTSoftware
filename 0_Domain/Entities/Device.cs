using ArandanoIRT.Web._0_Domain.Enums;

namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Stores physical monitoring hardware devices.
/// </summary>
public partial class Device
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? MacAddress { get; set; }

    public string? Description { get; set; }

    public int? PlantId { get; set; }

    public int CropId { get; set; }

    public short DataCollectionIntervalMinutes { get; set; }

    public DateTime RegisteredAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DeviceStatus Status { get; set; }

    public virtual Crop Crop { get; set; } = null!;

    public virtual ICollection<DeviceActivation> DeviceActivations { get; set; } = new List<DeviceActivation>();

    public virtual ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();

    public virtual ICollection<EnvironmentalReading> EnvironmentalReadings { get; set; } = new List<EnvironmentalReading>();

    public virtual Plant? Plant { get; set; }

    public virtual ICollection<ThermalCapture> ThermalCaptures { get; set; } = new List<ThermalCapture>();
}
