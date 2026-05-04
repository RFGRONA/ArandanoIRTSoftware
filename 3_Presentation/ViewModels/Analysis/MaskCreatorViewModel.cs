namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

/// <summary>
///     ViewModel para la página interactiva de creación de máscaras térmicas.
///     Provee a la vista todos los datos necesarios para renderizar la imagen, el mapa de calor y el editor de máscaras.
/// </summary>
public class MaskCreatorViewModel
{
    public int PlantId { get; set; }
    public string PlantName { get; set; }

    /// <summary>
    ///     Ruta a la imagen RGB de referencia para la creación de la máscara.
    /// </summary>
    public string? RgbImagePath { get; set; }

    /// <summary>
    ///     La matriz de temperaturas de la captura térmica.
    /// </summary>
    public List<float?>? Temperatures { get; set; }

    /// <summary>
    ///     Las coordenadas de una máscara existente, en formato JSON, para precargar en el editor.
    /// </summary>
    public string ExistingMaskJson { get; set; } = "[]";

    public float MinTemp { get; set; }
    public float MaxTemp { get; set; }
    public int ThermalImageWidth => 32;
    public int ThermalImageHeight => 24;
}