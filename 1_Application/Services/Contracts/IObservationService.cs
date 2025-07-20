using System.Security.Claims;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._3_Presentation.ViewModels;
using ArandanoIRT.Web.Common;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IObservationService
{
    /// <summary>
    ///     Crea una nueva observación en la base de datos.
    /// </summary>
    /// <param name="model">Los datos de la nueva observación.</param>
    /// <param name="userPrincipal">El usuario que está creando la observación.</param>
    /// <returns>Un Result que indica si la operación fue exitosa.</returns>
    Task<Result> CreateObservationAsync(ObservationCreateDto model, ClaimsPrincipal userPrincipal);

    /// <summary>
    ///     Obtiene una lista paginada de todas las observaciones.
    /// </summary>
    /// <param name="pageNumber">El número de página a recuperar.</param>
    /// <param name="pageSize">El tamaño de la página.</param>
    /// <returns>Una lista paginada de observaciones.</returns>
    Task<PagedResultDto<ObservationListDto>> GetPagedObservationsAsync(ObservationQueryFilters filters);
}