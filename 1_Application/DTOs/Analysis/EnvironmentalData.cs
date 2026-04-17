namespace ArandanoIRT.Web._1_Application.DTOs.Analysis;

/// <summary>
/// Contiene los datos ambientales consolidados necesarios para un ciclo de análisis de estrés hídrico.
/// </summary>
public class EnvironmentalData
{
    /// <summary>
    /// Indica si las condiciones ambientales son adecuadas para un cálculo válido del CWSI.
    /// </summary>
    public bool IsConditionSuitable { get; init; }

    /// <summary>
    /// El Déficit de Presión de Vapor (VPD) calculado en kilopascales (kPa).
    /// </summary>
    public double VpdKpa { get; init; }

    /// <summary>
    /// La temperatura ambiente en Celsius utilizada para los cálculos.
    /// </summary>
    public double AmbientTemperatureC { get; init; }

    /// <summary>
    /// El porcentaje de humedad ambiental utilizado para los cálculos.
    /// </summary>
    public double AmbientHumidity { get; init; }
}