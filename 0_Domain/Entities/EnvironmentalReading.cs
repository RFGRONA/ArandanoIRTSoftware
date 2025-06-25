namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Stores environmental data collected by device sensors.
/// </summary>
public partial class EnvironmentalReading
{
    public long Id { get; set; }

    public int DeviceId { get; set; }

    public int? PlantId { get; set; }

    public float Temperature { get; set; }

    public float Humidity { get; set; }

    public float? CityTemperature { get; set; }

    public float? CityHumidity { get; set; }

    public string? CityWeatherCondition { get; set; }

    public string? ExtraData { get; set; }

    public DateTime RecordedAtServer { get; set; }

    public DateTime? RecordedAtDevice { get; set; }

    public virtual Device Device { get; set; } = null!;

    public virtual Plant? Plant { get; set; }
}
