using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace ArandanoIRT.Web.Data.Models;

[Table("sensor_data")]
public class SensorDataModel : BaseModel
{
    [PrimaryKey("id", false)] // false porque es BIGSERIAL
    public long Id { get; set; }

    [Column("device_id")]
    public int DeviceId { get; set; }

    [Column("plant_id")]
    public int? PlantId { get; set; }

    [Column("crop_id")]
    public int? CropId { get; set; }

    [Column("light")] // Coincide con el nombre de columna en Supabase (era lightIntensity en el plan original)
    public float? Light { get; set; } // Nullable si puede no venir

    [Column("temperature")]
    public float Temperature { get; set; }

    [Column("humidity")]
    public float Humidity { get; set; }

    [Column("city_temperature")]
    public float? CityTemperature { get; set; }

    [Column("city_humidity")]
    public float? CityHumidity { get; set; }

    [Column("is_night")]
    public bool? IsNight { get; set; }

    [Column("recorded_at")]
    public DateTime RecordedAt { get; set; }
    
    [Column("city_weather_condition")] 
    public string? CityWeatherCondition { get; set; }
}