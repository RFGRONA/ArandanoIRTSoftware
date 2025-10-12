namespace ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

/// <summary>
///     ViewModel para la página de detalles de análisis de una planta específica.
///     Contiene los datos necesarios para renderizar los gráficos y mostrar la información del período.
/// </summary>
public class AnalysisDetailsViewModel
{
    public int PlantId { get; set; }
    public string PlantName { get; set; }
    public string CropName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>
    ///     Indica si se encontraron datos de análisis para el período seleccionado.
    /// </summary>
    public bool HasData { get; set; }

    /// <summary>
    ///     Los datos del gráfico de CWSI, pre-serializados como una cadena JSON para Chart.js.
    /// </summary>
    public string CwsiChartDataJson { get; set; }

    /// <summary>
    ///     Los datos del gráfico de temperaturas, pre-serializados como una cadena JSON para Chart.js.
    /// </summary>
    public string TempChartDataJson { get; set; }

    public float CwsiThresholdIncipient { get; set; }
    public float CwsiThresholdCritical { get; set; }
}