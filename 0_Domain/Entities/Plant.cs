namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Stores data for each monitored plant.
/// </summary>
public partial class Plant
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int CropId { get; set; }

    public DateTime RegisteredAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Crop Crop { get; set; } = null!;

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();

    public virtual ICollection<EnvironmentalReading> EnvironmentalReadings { get; set; } = new List<EnvironmentalReading>();

    public virtual ICollection<Observation> Observations { get; set; } = new List<Observation>();

    public virtual ICollection<ThermalCapture> ThermalCaptures { get; set; } = new List<ThermalCapture>();
}
