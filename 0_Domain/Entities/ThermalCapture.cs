namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Almacena las capturas termográficas. Las estadísticas se guardan en formato JSONB
/// y la ruta de la imagen apunta a un servicio de almacenamiento de objetos.
/// </summary>
public partial class ThermalCapture
{
    public long Id { get; set; }

    public int DeviceId { get; set; }

    public int? PlantId { get; set; }

    public string ThermalDataStats { get; set; } = null!;

    public string? RgbImagePath { get; set; }

    public DateTime RecordedAtServer { get; set; }

    public DateTime? RecordedAtDevice { get; set; }

    public virtual Device Device { get; set; } = null!;

    public virtual Plant? Plant { get; set; }
}