using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para el servicio de analíticas.
///     Este servicio es responsable de preparar, procesar y entregar los datos
///     que se visualizarán en los dashboards y vistas de análisis de la aplicación.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    ///     Guarda las coordenadas de la máscara térmica (el área de interés) para una planta específica.
    /// </summary>
    /// <param name="plantId">El ID de la planta a la que se le asociará la máscara.</param>
    /// <param name="maskCoordinatesJson">Una cadena JSON que representa las coordenadas de la máscara.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la operación fue exitosa.</returns>
    Task<Result> SaveThermalMaskAsync(int plantId, string maskCoordinatesJson);

    /// <summary>
    ///     Obtiene los datos necesarios para renderizar la vista de monitoreo principal, que muestra el estado de todos los
    ///     cultivos.
    /// </summary>
    /// <returns>Un objeto <c>Result</c> que contiene una lista de <c>CropMonitorViewModel</c> si la operación es exitosa.</returns>
    Task<Result<List<CropMonitorViewModel>>> GetCropsForMonitoringAsync();

    /// <summary>
    ///     Obtiene todos los datos de análisis detallados para una planta específica dentro de un rango de fechas.
    /// </summary>
    /// <param name="plantId">El ID de la planta a consultar.</param>
    /// <param name="startDate">La fecha de inicio del rango de consulta (opcional).</param>
    /// <param name="endDate">La fecha de fin del rango de consulta (opcional).</param>
    /// <returns>
    ///     Un objeto <c>Result</c> que contiene el <c>AnalysisDetailsViewModel</c> con todos los datos para la vista de
    ///     detalles.
    /// </returns>
    Task<Result<AnalysisDetailsViewModel>> GetAnalysisDetailsAsync(int plantId, DateTime? startDate, DateTime? endDate);
}