using System.Security.Claims;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Admin;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface ISupportService
{
    Task<Result> ProcessPublicHelpRequestAsync(PublicHelpRequestDto model);
    Task<Result> ProcessAuthenticatedHelpRequestAsync(AuthenticatedHelpRequestDto model, ClaimsPrincipal userPrincipal);
}