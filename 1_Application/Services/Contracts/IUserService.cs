using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IUserService
{
    Task<IEnumerable<SelectListItem>> GetUsersForSelectionAsync();
}