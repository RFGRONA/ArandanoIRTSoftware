using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;
using ArandanoIRT.Web._1_Application.DTOs.Common;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
using ArandanoIRT.Web._1_Application.DTOs.SensorData;
using ArandanoIRT.Web._3_Presentation.ViewModels.SensorData;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public class DataQueryFilters // DTO para filtros comunes
{
    public int? DeviceId { get; set; }
    public int? PlantId { get; set; }
    public int? CropId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? LogLevel { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
///     Define el contrato para el servicio de consulta de datos.
///     Es responsable de todas las operaciones de lectura y recuperación de datos de sensores,
///     capturas térmicas y otra información relevante para la visualización y el análisis.
/// </summary>
public interface IDataQueryService
{
    /// <summary>
    ///     Obtiene una lista paginada de datos de sensores ambientales.
    /// </summary>
    /// <param name="filters">Los filtros a aplicar en la consulta.</param>
    /// <returns>Un resultado paginado de <c>SensorDataDisplayDto</c>.</returns>
    Task<Result<PagedResultDto<SensorDataDisplayDto>>> GetSensorDataAsync(DataQueryFilters filters);
    /// <summary>
    ///     Obtiene una lista paginada de capturas termográficas.
    /// </summary>
    /// <param name="filters">Los filtros a aplicar en la consulta.</param>
    /// <returns>Un resultado paginado de <c>ThermalCaptureSummaryDto</c>.</returns>
    Task<Result<PagedResultDto<ThermalCaptureSummaryDto>>> GetThermalCapturesAsync(DataQueryFilters filters);
    /// <summary>
    ///     Obtiene los detalles completos de una captura termográfica específica.
    /// </summary>
    /// <param name="captureId">El ID de la captura a buscar.</param>
    /// <returns>Un <c>ThermalCaptureDetailsDto</c> con los detalles completos o null si no se encuentra.</returns>
    Task<Result<ThermalCaptureDetailsDto?>> GetThermalCaptureDetailsAsync(long captureId);

    /// <summary>
    ///     Obtiene los datos ambientales necesarios para los gráficos del dashboard principal.
    /// </summary>
    /// <param name="duration">El período de tiempo a consultar (ej. últimas 24 horas).</param>
    /// <param name="cropId">ID del cultivo para filtrar (opcional).</param>
    /// <param name="plantId">ID de la planta para filtrar (opcional).</param>
    /// <returns>Una colección de <c>SensorDataDisplayDto</c>.</returns>
    Task<Result<IEnumerable<SensorDataDisplayDto>>> GetAmbientDataForDashboardAsync(TimeSpan duration, int? cropId,
        int? plantId);

    /// <summary>
    ///     Obtiene las estadísticas térmicas (min, max, prom) para los gráficos del dashboard.
    /// </summary>
    /// <param name="duration">El período de tiempo a consultar.</param>
    /// <param name="cropId">ID del cultivo para filtrar (opcional).</param>
    /// <param name="plantId">ID de la planta para filtrar (opcional).</param>
    /// <returns>Un <c>ThermalStatsDto</c> con las estadísticas.</returns>
    Task<Result<ThermalStatsDto>> GetThermalStatsForDashboardAsync(TimeSpan duration, int? cropId, int? plantId);
    /// <summary>
    ///     Obtiene el número total de dispositivos activos.
    /// </summary>
    /// <param name="cropId">ID del cultivo para filtrar (opcional).</param>
    /// <param name="plantId">ID de la planta para filtrar (opcional).</param>
    /// <returns>El conteo de dispositivos activos.</returns>
    Task<Result<int>> GetActiveDevicesCountAsync(int? cropId, int? plantId);
    /// <summary>
    ///     Obtiene el número total de plantas monitoreadas.
    /// </summary>
    /// <param name="cropId">ID del cultivo para filtrar (opcional).</param>
    /// <returns>El conteo de plantas monitoreadas.</returns>
    Task<Result<int>> GetMonitoredPlantsCountAsync(int? cropId);
    /// <summary>
    ///     Obtiene la lectura ambiental más reciente.
    /// </summary>
    /// <param name="cropId">ID del cultivo para filtrar (opcional).</param>
    /// <param name="plantId">ID de la planta para filtrar (opcional).</param>
    /// <param name="deviceId">ID del dispositivo para filtrar (opcional).</param>
    /// <returns>El <c>SensorDataDisplayDto</c> más reciente o null.</returns>
    Task<Result<SensorDataDisplayDto?>> GetLatestAmbientDataAsync(int? cropId, int? plantId, int? deviceId);

    /// <summary>
    ///     Obtiene todos los datos crudos (ambientales y térmicos) necesarios para ejecutar un análisis de estrés hídrico.
    /// </summary>
    /// <param name="plantIds">La lista de IDs de las plantas a consultar.</param>
    /// <param name="startTime">La fecha y hora de inicio del período.</param>
    /// <param name="endTime">La fecha y hora de fin del período.</param>
    /// <returns>Una lista de <c>PlantRawDataDto</c>, cada uno con los datos crudos de una planta.</returns>
    Task<Result<List<PlantRawDataDto>>> GetRawDataForAnalysisAsync(List<int> plantIds, DateTime startTime,
        DateTime endTime);

    /// <summary>
    ///     Obtiene la captura térmica más reciente de una planta, usada para la creación de máscaras térmicas.
    /// </summary>
    /// <param name="plantId">El ID de la planta.</param>
    /// <returns>Una tupla con las estadísticas térmicas y la ruta de la imagen.</returns>
    Task<Result<(ThermalDataDto? Stats, string? ImagePath)>> GetLatestCaptureForMaskAsync(int plantId);
    /// <summary>
    ///     Exporta los datos de sensores ambientales a un archivo CSV.
    /// </summary>
    /// <param name="filters">Los filtros a aplicar en la consulta.</param>
    /// <returns>Un arreglo de bytes que representa el archivo CSV.</returns>
    Task<byte[]> GetAmbientDataAsCsvAsync(DataQueryFilters filters);
    /// <summary>
    ///     Exporta los datos de capturas termográficas a un archivo CSV.
    /// </summary>
    /// <param name="filters">Los filtros a aplicar en la consulta.</param>
    /// <returns>Un arreglo de bytes que representa el archivo CSV.</returns>
    Task<byte[]> GetThermalCapturesAsCsvAsync(DataQueryFilters filters);
}