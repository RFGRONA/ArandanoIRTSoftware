using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IUserService
{
    Task<IEnumerable<SelectListItem>> GetUsersForSelectionAsync();
    Task<Result> RegisterUserAsync(RegisterDto model);
    Task<SignInResult> LoginUserAsync(LoginDto model);
}