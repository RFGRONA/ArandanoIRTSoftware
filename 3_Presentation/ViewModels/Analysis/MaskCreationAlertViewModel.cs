namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

/// <summary>
///     ViewModel para la plantilla de correo electrónico que notifica sobre la necesidad de crear máscaras térmicas.
/// </summary>
public class MaskCreationAlertViewModel
{
    public string UserName { get; set; }

    /// <summary>
    ///     Lista de nombres de las plantas que requieren una máscara.
    /// </summary>
    public List<string> PlantNames { get; set; }

    /// <summary>
    ///     URL del botón de llamada a la acción en el correo.
    /// </summary>
    public string CtaButtonUrl { get; set; } = "/Admin/Plants/Index";
}