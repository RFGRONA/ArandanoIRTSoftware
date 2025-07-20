using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IInvitationService
{
    Task<Result<InvitationCode>> CreateInvitationAsync(string email, bool isAdmin, int? createdByUserId);
    Task<Result<InvitationCode>> ValidateCodeAsync(string code);
    Task<Result> MarkCodeAsUsedAsync(int invitationId);
}