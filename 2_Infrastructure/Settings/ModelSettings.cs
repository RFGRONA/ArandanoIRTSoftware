namespace ArandanoIRT.Web._2_Infrastructure.Settings;

/// <summary>
///     Contiene la configuración relacionada con el modelo de machine learning.
///     Estas propiedades se cargan desde la sección "ModelSettings" en appsettings.json.
/// </summary>
public class ModelSettings
{
    /// <summary>
    ///     Define el nombre de la sección en el archivo appsettings.json.
    /// </summary>
    public const string SectionName = "ModelSettings";

    /// <summary>
    ///     La ruta relativa al archivo del modelo en formato ONNX.
    /// </summary>
    public string OnnxModelPath { get; set; } = string.Empty;
}