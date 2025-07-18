using System.Security.Claims;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web.Common;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface ISupportService
{
    Task<Result> ProcessPublicHelpRequestAsync(PublicHelpRequestDto model);
    Task<Result> ProcessAuthenticatedHelpRequestAsync(AuthenticatedHelpRequestDto model, ClaimsPrincipal userPrincipal);
}