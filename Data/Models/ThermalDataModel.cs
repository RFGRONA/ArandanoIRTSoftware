using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace ArandanoIRT.Web.Data.Models;

[Table("thermal_data")]
public class ThermalDataModel : BaseModel
{
    [PrimaryKey("id", false)] // false porque es BIGSERIAL
    public long Id { get; set; }

    [Column("device_id")]
    public int DeviceId { get; set; }

    [Column("plant_id")]
    public int? PlantId { get; set; }

    [Column("crop_id")]
    public int? CropId { get; set; }

    // thermal_image_data es JSONB en Supabase.
    // supabase-csharp puede mapearlo a un string (y tú deserializas/serializas)
    // o directamente a un tipo JSON si es compatible (ej: JsonNode de System.Text.Json, o JObject de Newtonsoft.Json)
    // Por simplicidad y para ser explícitos, usar string aquí y manejar la serialización en el servicio.
    [Column("thermal_image_data")]
    public string ThermalImageData { get; set; } = string.Empty; // Almacenará el JSON como string

    [Column("rgb_image_path")]
    public string? RgbImagePath { get; set; } // Ruta al archivo en Supabase Storage

    [Column("recorded_at")]
    public DateTime RecordedAt { get; set; }
}