namespace ArandanoIRT.Web._3_Presentation.ViewModels.Emails;

public class AdminDeletionRequestViewModel
{
    public string RecipientName { get; set; }
    public string InitiatingAdminName { get; set; }
    public string AdminToDeleteName { get; set; }
    public string ConfirmationLink { get; set; }
}