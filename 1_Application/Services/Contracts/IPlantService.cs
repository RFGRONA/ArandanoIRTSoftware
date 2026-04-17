using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.DTOs.Common;
using ArandanoIRT.Web._1_Application.DTOs.Plants;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para el servicio de gestión de plantas.
///     Es responsable de todas las operaciones CRUD y la lógica de negocio asociada a las plantas.
/// </summary>
public interface IPlantService
{
    /// <summary>
    ///     Obtiene una lista resumida de todas las plantas registradas.
    /// </summary>
    /// <returns>Un objeto <c>Result</c> que contiene una colección de <c>PlantSummaryDto</c>.</returns>
    Task<Result<IEnumerable<PlantSummaryDto>>> GetAllPlantsAsync();
    Task<Result<PagedResultDto<PlantSummaryDto>>> GetPagedPlantsAsync(PlantQueryFilters filters);
    /// <summary>
    ///     Obtiene los detalles completos de una planta específica por su ID.
    /// </summary>
    /// <param name="plantId">El ID de la planta a buscar.</param>
    /// <returns>Un objeto <c>Result</c> que contiene el <c>PlantDetailsDto</c> o null si no se encuentra.</returns>
    Task<Result<PlantDetailsDto?>> GetPlantByIdAsync(int plantId);
    /// <summary>
    ///     Obtiene los datos de una planta, formateados para poblar un formulario de edición.
    /// </summary>
    /// <param name="plantId">El ID de la planta a editar.</param>
    /// <returns>Un objeto <c>Result</c> que contiene el <c>PlantEditDto</c> o null si no se encuentra.</returns>
    Task<Result<PlantEditDto?>> GetPlantForEditByIdAsync(int plantId);
    /// <summary>
    ///     Crea una nueva planta en la base de datos.
    /// </summary>
    /// <param name="plantDto">El DTO con la información de la nueva planta.</param>
    /// <returns>Un objeto <c>Result</c> que contiene el ID de la planta recién creada.</returns>
    Task<Result<int>> CreatePlantAsync(PlantCreateDto plantDto);
    /// <summary>
    ///     Actualiza la información de una planta existente.
    /// </summary>
    /// <param name="plantDto">El DTO con los datos actualizados de la planta.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la operación fue exitosa.</returns>
    Task<Result> UpdatePlantAsync(PlantEditDto plantDto);
    /// <summary>
    ///     Elimina una planta de la base de datos.
    /// </summary>
    /// <param name="plantId">El ID de la planta a eliminar.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la operación fue exitosa.</returns>
    Task<Result> DeletePlantAsync(int plantId);
    /// <summary>
    ///     Obtiene una lista resumida de todas las plantas que pertenecen a un cultivo específico.
    /// </summary>
    /// <param name="cropId">El ID del cultivo.</param>
    /// <returns>Un objeto <c>Result</c> que contiene una colección de <c>PlantSummaryDto</c>.</returns>
    Task<Result<IEnumerable<PlantSummaryDto>>> GetPlantsByCropAsync(int cropId);
    /// <summary>
    ///     Actualiza el estado de una planta y registra el cambio en el historial.
    /// </summary>
    /// <param name="plantId">El ID de la planta a actualizar.</param>
    /// <param name="newStatus">El nuevo estado a asignar.</param>
    /// <param name="observation">Una observación opcional sobre el cambio de estado.</param>
    /// <param name="userId">El ID del usuario que realiza el cambio.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la operación fue exitosa.</returns>
    Task<Result> UpdatePlantStatusAsync(int plantId, PlantStatus newStatus, string? observation, int userId);
    /// <summary>
    ///     Obtiene una lista de todas las plantas para usar en un control de selección (dropdown).
    /// </summary>
    /// <returns>Una colección de <c>SelectListItem</c>.</returns>
    Task<IEnumerable<SelectListItem>> GetPlantsForSelectionAsync();

    /// <summary>
    ///     Obtiene el historial de cambios de estado, con la posibilidad de filtrar por planta, usuario y rango de fechas.
    /// </summary>
    /// <param name="plantId">ID de la planta para filtrar (opcional).</param>
    /// <param name="userId">ID del usuario para filtrar (opcional).</param>
    /// <param name="startDate">Fecha de inicio para filtrar (opcional).</param>
    /// <param name="endDate">Fecha de fin para filtrar (opcional).</param>
    /// <returns>Una colección de <c>PlantStatusHistoryDto</c>.</returns>
    Task<IEnumerable<PlantStatusHistoryDto>> GetPlantStatusHistoryAsync(int? plantId, int? userId, DateTime? startDate,
        DateTime? endDate);

    // Métodos para poblar dropdowns
    /// <summary>
    ///     Obtiene una lista de todos los cultivos para usar en un control de selección (dropdown).
    /// </summary>
    /// <returns>Una colección de <c>SelectListItem</c>.</returns>
    Task<IEnumerable<SelectListItem>> GetCropsForSelectionAsync();
    /// <summary>
    ///     Obtiene una lista de los grupos experimentales para usar en un control de selección (dropdown).
    /// </summary>
    /// <returns>Una colección de <c>SelectListItem</c>.</returns>
    IEnumerable<SelectListItem> GetExperimentalGroupsForSelection();
}