namespace ArandanoIRT.Web._1_Application.DTOs.Analysis;

/// <summary>
///     Holds the consolidated environmental data required for a water stress analysis cycle.
/// </summary>
public class EnvironmentalData
{
    /// <summary>
    ///     Indicates if the environmental conditions are suitable for a valid CWSI calculation.
    /// </summary>
    public bool IsConditionSuitable { get; init; }

    /// <summary>
    ///     The calculated Vapor Pressure Deficit (VPD) in kilopascals (kPa).
    /// </summary>
    public double VpdKpa { get; init; }

    /// <summary>
    ///     The ambient temperature in Celsius used for the calculations.
    /// </summary>
    public double AmbientTemperatureC { get; init; }

    /// <summary>
    ///     The ambient humidity percentage used for the calculations.
    /// </summary>
    public double AmbientHumidity { get; init; }
}