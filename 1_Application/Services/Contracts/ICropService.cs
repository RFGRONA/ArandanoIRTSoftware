using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Common;
using ArandanoIRT.Web._1_Application.DTOs.Crops;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para el servicio de gestión de cultivos (Crops).
///     Es responsable de todas las operaciones CRUD (Crear, Leer, Actualizar, Eliminar) para los cultivos.
/// </summary>
public interface ICropService
{
    /// <summary>
    ///     Obtiene una lista resumida de todos los cultivos registrados en el sistema.
    /// </summary>
    /// <returns>Un objeto <c>Result</c> que contiene una colección de <c>CropSummaryDto</c>.</returns>
    Task<Result<IEnumerable<CropSummaryDto>>> GetAllCropsAsync();
    Task<Result<PagedResultDto<CropSummaryDto>>> GetPagedCropsAsync(CropQueryFilters filters);
    Task<Result<CropDetailsDto?>> GetCropByIdAsync(int cropId); // Nullable si no se encuentra
    Task<Result<CropEditDto?>> GetCropForEditByIdAsync(int cropId); // Para poblar el DTO de edición
    /// <summary>
    ///     Crea un nuevo cultivo en la base de datos a partir de los datos proporcionados.
    /// </summary>
    /// <param name="cropDto">El DTO con la información del nuevo cultivo.</param>
    /// <returns>Un objeto <c>Result</c> que contiene el ID del cultivo recién creado si la operación fue exitosa.</returns>
    Task<Result<int>> CreateCropAsync(CropCreateDto cropDto);
    /// <summary>
    ///     Actualiza la información de un cultivo existente.
    /// </summary>
    /// <param name="cropDto">El DTO con los datos actualizados del cultivo.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la operación fue exitosa.</returns>
    Task<Result> UpdateCropAsync(CropEditDto cropDto);
    /// <summary>
    ///     Elimina un cultivo de la base de datos.
    /// </summary>
    /// <param name="cropId">El ID del cultivo a eliminar.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la operación fue exitosa.</returns>
    Task<Result> DeleteCropAsync(int cropId);
    /// <summary>
    ///     Obtiene específicamente los parámetros de análisis para un cultivo.
    /// </summary>
    /// <param name="cropId">El ID del cultivo.</param>
    /// <returns>Un objeto <c>Result</c> que contiene la configuración de análisis del cultivo.</returns>
    Task<Result<CropSettings>> GetAnalysisParametersAsync(int cropId);
}