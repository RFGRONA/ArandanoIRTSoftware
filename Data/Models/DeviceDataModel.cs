using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace ArandanoIRT.Web.Data.Models;

[Table("device_data")]
public class DeviceDataModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("plant_id")]
    public int? PlantId { get; set; } // Nullable FK

    [Column("crop_id")]
    public int? CropId { get; set; } // Nullable FK

    [Column("status_id")]
    public int StatusId { get; set; } // FK, asumimos que siempre hay un estado

    [Column("data_collection_time_minutes")]
    public short DataCollectionTimeMinutes { get; set; }

    [Column("registered_at")]
    public DateTime RegisteredAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("mac_address")]
    public string? MacAddress { get; set; }
}