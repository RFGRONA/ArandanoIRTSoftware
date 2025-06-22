using System.ComponentModel.DataAnnotations.Schema;

namespace ArandanoIRT.Web._0_Domain.Entities;

[Table("thermal_capture")]
public class ThermalCapture
{
    public long Id { get; set; }
    public int DeviceId { get; set; }
    public int? PlantId { get; set; }
    public int? CropId { get; set; }
    public string ThermalImageData { get; set; } = string.Empty; 
    public string? RgbImagePath { get; set; } 
    public DateTime RecordedAt { get; set; }
    
    // Navigation Properties
    public virtual Device Device { get; set; }
    public virtual Plant? Plant { get; set; }
}