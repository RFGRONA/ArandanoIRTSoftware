using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para el servicio que encapsula la lógica principal para ejecutar los cálculos de análisis de
///     estrés hídrico.
///     Este servicio es utilizado tanto por los trabajos en segundo plano en tiempo real como por procesos de re-análisis
///     histórico.
/// </summary>
public interface IAnalysisExecutionService
{
    /// <summary>
    ///     Ejecuta un proceso de re-análisis ("catch-up") para una planta, procesando todos sus datos históricos crudos
    ///     y guardando los resultados en la base de datos. Está diseñado para ser llamado por un trabajo asíncrono (ej.
    ///     Hangfire).
    /// </summary>
    /// <param name="plantId">El ID de la planta que se va a re-analizar.</param>
    Task ExecuteCatchUpForPlantAsync(int plantId);

    /// <summary>
    ///     Calcula un único resultado de análisis (CWSI) basado en un conjunto específico de datos crudos.
    ///     Este método es una función pura: no tiene efectos secundarios como guardar en la base de datos.
    /// </summary>
    /// <param name="input">Los datos crudos de entrada necesarios para el cálculo.</param>
    /// <returns>
    ///     Un objeto <c>Result</c> que contiene el <c>AnalysisResult</c> si el cálculo es exitoso, o un error si no lo
    ///     es.
    /// </returns>
    Task<Result<AnalysisResult>> CalculateCwsiAsync(CwsiCalculationInput input);

    /// <summary>
    ///     DTO que encapsula todos los datos de entrada necesarios para un único cálculo del CWSI.
    /// </summary>
    /// <param name="EnvironmentalReading">Lectura de las condiciones ambientales en el momento de la captura.</param>
    /// <param name="MonitoredPlantCapture">Captura térmica de la planta que se está monitoreando.</param>
    /// <param name="ControlPlantCapture">Captura térmica de la planta de control (referencia bien regada).</param>
    /// <param name="MonitoredPlant">Entidad de la planta monitoreada.</param>
    /// <param name="ControlPlant">Entidad de la planta de control.</param>
    record CwsiCalculationInput(
        EnvironmentalReading EnvironmentalReading,
        ThermalCapture MonitoredPlantCapture,
        Plant MonitoredPlant,
        AnalysisParameters Parameters
    );
}