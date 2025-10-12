using ArandanoIRT.Web._0_Domain.Entities;

namespace ArandanoIRT.Web._1_Application.DTOs.Analysis;

/// <summary>
/// Agrupa todos los datos crudos de una planta necesarios para ejecutar un análisis.
/// </summary>
public class PlantRawDataDto
{
    public Plant Plant { get; set; }
    public List<EnvironmentalReading> EnvironmentalReadings { get; set; }
    public List<ThermalCapture> ThermalCaptures { get; set; }
}