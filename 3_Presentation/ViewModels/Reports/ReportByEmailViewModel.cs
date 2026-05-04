namespace ArandanoIRT.Web._3_Presentation.ViewModels.Reports;

/// <summary>
///     ViewModel para la plantilla de correo utilizada cuando se envía un informe PDF como archivo adjunto.
/// </summary>
public class ReportByEmailViewModel
{
    public string PlantName { get; set; }
    public DateTime? GenerationDate { get; set; } = DateTime.UtcNow;
}