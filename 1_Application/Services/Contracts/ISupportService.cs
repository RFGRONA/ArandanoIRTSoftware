using System.Security.Claims;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Admin;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
/// Define el contrato para el servicio de soporte técnico.
/// Es responsable de procesar las solicitudes de ayuda enviadas tanto por usuarios públicos como por usuarios autenticados.
/// </summary>
public interface ISupportService
{
    /// <summary>
    /// Procesa una solicitud de ayuda enviada desde el formulario público (por un usuario no autenticado).
    /// </summary>

    /// <param name="model">El DTO con los datos de la solicitud de ayuda pública.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la solicitud se procesó con éxito.</returns>
    Task<Result> ProcessPublicHelpRequestAsync(PublicHelpRequestDto model);

    /// <summary>
    /// Procesa una solicitud de ayuda enviada por un usuario que ya ha iniciado sesión en el sistema.
    /// </summary>
    /// <param name="model">El DTO con los datos de la solicitud de ayuda.</param>
    /// <param name="userPrincipal">El ClaimsPrincipal del usuario autenticado que realiza la solicitud.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la solicitud se procesó con éxito.</returns>
    Task<Result> ProcessAuthenticatedHelpRequestAsync(AuthenticatedHelpRequestDto model, ClaimsPrincipal userPrincipal);
}