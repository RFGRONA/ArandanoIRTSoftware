using ArandanoIRT.Web._0_Domain.Enums;

namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Almacena los resultados calculados del módulo de análisis para una planta específica.
/// Facilita la generación de informes y la auditoría completa de los datos.
/// </summary>
public class AnalysisResult
{
    public long Id { get; set; }
    public int PlantId { get; set; }
    public DateTime RecordedAt { get; set; }
    public float? CwsiValue { get; set; }
    public PlantStatus Status { get; set; }
    public float? CanopyTemperature { get; set; }
    public float? AmbientTemperature { get; set; }
    public float? Vpd { get; set; }
    public float? BaselineTwet { get; set; }
    public float? BaselineTdry { get; set; }

    public virtual Plant Plant { get; set; } = null!;
}