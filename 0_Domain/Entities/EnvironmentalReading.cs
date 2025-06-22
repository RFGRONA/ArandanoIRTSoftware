using System.ComponentModel.DataAnnotations.Schema;

namespace ArandanoIRT.Web._0_Domain.Entities;

[Table("environmental_reading")]
public class EnvironmentalReading
{
    public long Id { get; set; }
    public int DeviceId { get; set; }
    public int? PlantId { get; set; }
    public int? CropId { get; set; }
    public float? Light { get; set; }
    public float Temperature { get; set; }
    public float Humidity { get; set; }
    public float? CityTemperature { get; set; }
    public float? CityHumidity { get; set; }
    public bool? IsNight { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? CityWeatherCondition { get; set; }
    
    // Navigation Properties
    public virtual Device Device { get; set; }
    public virtual Plant? Plant { get; set; }
}