using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Encapsula la lógica principal para ejecutar los cálculos de análisis de estrés hídrico.
///     Este servicio es utilizado tanto por los trabajos de fondo en tiempo real como por el proceso de catch-up.
/// </summary>
public interface IAnalysisExecutionService
{
    /// <summary>
    ///     Ejecuta un proceso de catch-up para una planta, analizando todos sus datos históricos crudos
    ///     y guardando los resultados en la base de datos. Diseñado para ser llamado por un trabajo de fondo asíncrono (ej.
    ///     Hangfire).
    /// </summary>
    /// <param name="plantId">El ID de la planta a analizar.</param>
    Task ExecuteCatchUpForPlantAsync(int plantId);

    /// <summary>
    ///     Calcula un único resultado de análisis basado en un conjunto de datos crudos.
    ///     Este método es una función pura: no guarda nada en la base de datos.
    /// </summary>
    /// <param name="input">Los datos crudos necesarios para el cálculo.</param>
    /// <returns>Un objeto AnalysisResult si el cálculo es exitoso, o un error si no lo es.</returns>
    Task<Result<AnalysisResult>> CalculateCwsiAsync(CwsiCalculationInput input);

    /// <summary>
    ///     DTO para encapsular todos los datos necesarios para un único cálculo de CWSI.
    /// </summary>
    record CwsiCalculationInput(
        EnvironmentalReading EnvironmentalReading,
        ThermalCapture MonitoredPlantCapture,
        ThermalCapture ControlPlantCapture,
        Plant MonitoredPlant,
        Plant ControlPlant
    );
}