namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para un servicio encargado de generar documentos en formato PDF.
/// </summary>
public interface IPdfGeneratorService
{
    /// <summary>
    ///     Genera un informe completo de una planta para un rango de fechas específico en formato PDF.
    /// </summary>
    /// <param name="plantId">El ID de la planta para la cual se generará el informe.</param>
    /// <param name="startDate">La fecha de inicio del período del informe.</param>
    /// <param name="endDate">La fecha de fin del período del informe.</param>
    /// <returns>Un arreglo de bytes que representa el contenido del archivo PDF generado.</returns>
    Task<byte[]> GeneratePlantReportAsync(int plantId, DateTime startDate, DateTime endDate);
}