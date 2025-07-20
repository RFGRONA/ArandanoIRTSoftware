using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IAlertService
{
    // --- Alertas de Seguridad ---
    Task TriggerFailedLoginAlertAsync(User user);

    // --- Notificaciones de Cuenta ---
    Task SendPasswordResetEmailAsync(string userEmail, string userName, string resetLink);
    Task SendPasswordChangedEmailAsync(string userEmail, string userName);

    // --- Notificaciones de Registro ---
    Task SendInvitationEmailAsync(string recipientEmail, string recipientName, InvitationCode invitation);

    // --- Notificaciones de Soporte ---
// --- CÓDIGO NUEVO ---
    Task SendPublicHelpRequestEmailAsync(PublicHelpRequestDto request, List<User> adminsToNotify);
    Task SendAuthenticatedHelpRequestEmailAsync(AuthenticatedHelpRequestDto request, User requestingUser, List<User> adminsToNotify);
}