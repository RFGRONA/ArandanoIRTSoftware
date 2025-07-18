namespace ArandanoIRT.Web._1_Application.DTOs.Admin;

public class ManageProfileViewModel
{
    public ProfileInfoDto ProfileInfo { get; set; } = new();
    public ChangePasswordDto ChangePassword { get; set; } = new();
}