using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ArandanoIRT.Web.Data.Models;

[Table("status")]
public class StatusModel : BaseModel
{
    [PrimaryKey("id", false)] // false porque es SERIAL y la DB lo genera
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }
}