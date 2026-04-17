namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Representa un cultivo. Actúa como la principal entidad de agrupación (tenant) en el sistema.
/// </summary>
public class Crop
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string CityName { get; set; } = null!;

    public CropSettings CropSettings { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();

    public virtual ICollection<Plant> Plants { get; set; } = new List<Plant>();
}