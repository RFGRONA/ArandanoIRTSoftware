using System.Linq.Expressions;
using System.Security.Claims;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IUserService
{
    Task<IEnumerable<SelectListItem>> GetUsersForSelectionAsync();
    Task<Result> RegisterUserAsync(RegisterDto model);
    Task<(SignInResult Result, bool JustLockedOut)> LoginUserAsync(LoginDto model);

    Task<Result<(string Name, string ResetLink)>> GeneratePasswordResetAsync(ForgotPasswordDto model,
        IUrlHelper urlHelper, string scheme);

    Task<Result> ResetPasswordAsync(ResetPasswordDto model);
    Task<Result> ChangePasswordAsync(ClaimsPrincipal userPrincipal, ChangePasswordDto model);
    Task<Result> UpdateProfileAsync(ClaimsPrincipal userPrincipal, ProfileInfoDto model);
    Task<List<User>> GetAdminsToNotifyAsync(Expression<Func<AccountSettings, bool>> predicate);
    Task<List<User>> GetAllUsersAsync();
    Task<Result<IEnumerable<UserDto>>> GetAllUsersForManagementAsync();
    Task<Result> PromoteToAdminAsync(int userIdToPromote);
    Task<Result> DeleteUserAsync(int userIdToDelete, int currentUserId);

    Task<Result<string>> InitiateAdminDeletionAsync(int adminToDeleteId, int currentAdminId,
        string currentAdminPassword, IUrlHelper urlHelper, string scheme);

    Task<Result> ConfirmAdminDeletionAsync(int adminToDeleteId, string token);
}