using ArandanoIRT.Web._1_Application.DTOs.Admin;

namespace ArandanoIRT.Web._3_Presentation.ViewModels.Admin;

public class ManageProfileViewModel
{
    public ProfileInfoDto ProfileInfo { get; set; } = new();
    public ChangePasswordDto ChangePassword { get; set; } = new();
}