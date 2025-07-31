namespace ArandanoIRT.Web._1_Application.DTOs.Analysis;

public class MaskCreatorViewModel
{
    public int PlantId { get; set; }
    public string PlantName { get; set; }
    public string? RgbImagePath { get; set; }
    public List<float?>? Temperatures { get; set; }
    public string ExistingMaskJson { get; set; } = "[]";
    public float MinTemp { get; set; }
    public float MaxTemp { get; set; }
    public int ThermalImageWidth => 32;
    public int ThermalImageHeight => 24;
}