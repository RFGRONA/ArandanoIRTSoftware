namespace ArandanoIRT.Web.Configuration;

public class AdminCredentialsSettings
{
    public const string SectionName = "AdminCredentials";
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // Cambiado a PasswordHash
}