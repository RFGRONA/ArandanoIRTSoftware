using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._3_Presentation.ViewModels.Alerts;
using ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IAlertService
{
    // --- Alertas de Seguridad ---
    Task TriggerFailedLoginAlertAsync(User user, string forgotPasswordUrl);

    // --- Notificaciones de Cuenta ---
    Task SendPasswordResetEmailAsync(string userEmail, string userName, string resetLink);
    Task SendPasswordChangedEmailAsync(string userEmail, string userName);
    Task SendInactivityWarningEmailAsync(User admin, int daysInactive, string loginUrl);
    Task SendAccountDeletedEmailAsync(string userEmail, string userName);

    Task SendAdminDeletionRequestEmailAsync(List<User> otherAdmins, string initiatingAdminName,
        string adminToDeleteName, string confirmationLink);

    // --- Notificaciones de Registro ---
    Task SendInvitationEmailAsync(string recipientEmail, string recipientName, InvitationCode invitation);

    // --- Notificaciones de Soporte ---
    Task SendPublicHelpRequestEmailAsync(PublicHelpRequestDto request, List<User> adminsToNotify);

    Task SendAuthenticatedHelpRequestEmailAsync(AuthenticatedHelpRequestDto request, User requestingUser,
        List<User> adminsToNotify);

    // --- Alerta Genérica ---
    Task SendGenericAlertEmailAsync(string email, string name, GenericAlertViewModel model);

    // --- Alertas de Análisis ---
    Task SendAnomalyAlertEmailAsync(string recipientEmail, AnomalyAlertViewModel viewModel);
    Task SendMaskCreationAlertEmailAsync(string recipientEmail, MaskCreationAlertViewModel viewModel);
    Task SendStressAlertEmailAsync(string recipientEmail, StressAlertViewModel viewModel);
    Task SendReportByEmailAsync(string recipientEmail, string plantName, byte[] pdfAttachment);
}