using ArandanoIRT.Web._1_Application.DTOs.Admin;

namespace ArandanoIRT.Web._3_Presentation.ViewModels.Admin;

public class UserManagementViewModel
{
    public List<UserDto> Users { get; set; } = new();
}