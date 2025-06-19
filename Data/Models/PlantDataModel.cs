using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace ArandanoIRT.Web.Data.Models;

[Table("plant_data")] // Asegúrate que el nombre de la tabla sea correcto
public class PlantDataModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("crop_id")]
    public int? CropId { get; set; } // Nullable FK

    [Column("status_id")]
    public int StatusId { get; set; } // FK

    [Column("registered_at")]
    public DateTime RegisteredAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

}