using System.ComponentModel.DataAnnotations.Schema;

namespace ArandanoIRT.Web._0_Domain.Entities;

[Table("plant_data")] 
public class Plant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? CropId { get; set; }
    public int StatusId { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public virtual Crop Crop { get; set; }
    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
    public virtual ICollection<EnvironmentalReading> EnvironmentalReadings { get; set; } = new List<EnvironmentalReading>();
    public virtual ICollection<ThermalCapture> ThermalCaptures { get; set; } = new List<ThermalCapture>();
    public virtual ICollection<Observation> Observations { get; set; } = new List<Observation>();
}