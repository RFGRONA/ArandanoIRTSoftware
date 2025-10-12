using System.Security.Claims;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Common;
using ArandanoIRT.Web._1_Application.DTOs.Observations;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para el servicio que gestiona las observaciones manuales de los agrónomos.
/// </summary>
public interface IObservationService
{
    /// <summary>
    ///     Crea una nueva observación en la base de datos.
    /// </summary>
    /// <param name="model">Los datos de la nueva observación a crear.</param>
    /// <param name="userPrincipal">El ClaimsPrincipal del usuario que está creando la observación.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la operación fue exitosa.</returns>
    Task<Result> CreateObservationAsync(ObservationCreateDto model, ClaimsPrincipal userPrincipal);

    /// <summary>
    ///     Obtiene una lista paginada y filtrada de todas las observaciones.
    /// </summary>
    /// <param name="filters">El objeto que contiene todos los filtros y parámetros de paginación.</param>
    /// <returns>Un objeto <c>PagedResultDto</c> con la lista paginada de observaciones.</returns>
    Task<PagedResultDto<ObservationListDto>> GetPagedObservationsAsync(ObservationQueryFilters filters);
}