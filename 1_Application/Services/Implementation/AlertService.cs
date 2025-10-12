using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.ViewModels.Alerts;
using ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;
using ArandanoIRT.Web._3_Presentation.ViewModels.Emails;
using ArandanoIRT.Web._3_Presentation.ViewModels.Reports;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

/// <summary>
///     Implementación del servicio de alertas y notificaciones.
///     Utiliza el IRazorViewToStringRenderer para convertir vistas Razor en HTML y el IEmailService para enviar los
///     correos.
/// </summary>
public class AlertService : IAlertService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<AlertService> _logger;
    private readonly IRazorViewToStringRenderer _razorRenderer;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="AlertService" />.
    /// </summary>
    /// <param name="emailService">El servicio para el envío de correos.</param>
    /// <param name="razorRenderer">El servicio para renderizar vistas Razor a string.</param>
    /// <param name="logger">El servicio de logging para registrar eventos y errores.</param>
    public AlertService(
        IEmailService emailService,
        IRazorViewToStringRenderer razorRenderer,
        ILogger<AlertService> logger)
    {
        _emailService = emailService;
        _razorRenderer = razorRenderer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task TriggerFailedLoginAlertAsync(User user, string forgotPasswordUrl)
    {
        try
        {
            var viewModel = new FailedLoginAlertViewModel
            {
                UserName = user.FirstName,
                AlertTime = DateTime.UtcNow,
                ForgotPasswordUrl = forgotPasswordUrl
            };

            var body = await _razorRenderer.RenderViewToStringAsync(
                "~/Views/Shared/EmailTemplates/_FailedLoginAlert.cshtml", viewModel);

            await _emailService.SendEmailAsync(user.Email, user.FirstName,
                "Alerta de Seguridad: Intentos de Inicio de Sesión Fallidos", body);
            _logger.LogInformation("Alerta de bloqueo de cuenta enviada a {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar la alerta de bloqueo de cuenta para el usuario {UserId}", user.Id);
        }
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(string userEmail, string userName, string resetLink)
    {
        var emailModel = (Name: userName, ResetLink: resetLink);
        var emailHtml =
            await _razorRenderer.RenderViewToStringAsync("/Views/Shared/EmailTemplates/_ForgotPasswordEmail.cshtml",
                emailModel);
        await _emailService.SendEmailAsync(userEmail, userName, "Restablece tu contraseña", emailHtml);
    }

    /// <inheritdoc />
    public async Task SendPasswordChangedEmailAsync(string userEmail, string userName)
    {
        var emailHtml =
            await _razorRenderer.RenderViewToStringAsync("/Views/Shared/EmailTemplates/_PasswordChangedEmail.cshtml",
                userName);
        await _emailService.SendEmailAsync(userEmail, userName, "Tu Contraseña ha sido Cambiada", emailHtml);
    }

    /// <inheritdoc />
    public async Task SendInvitationEmailAsync(string recipientEmail, string recipientName, InvitationCode invitation)
    {
        var emailHtml =
            await _razorRenderer.RenderViewToStringAsync("/Views/Shared/EmailTemplates/_InvitationEmail.cshtml",
                invitation);
        await _emailService.SendEmailAsync(recipientEmail, recipientName, "Invitación a Arandano IRT", emailHtml);
        _logger.LogInformation("Correo de invitación (ID: {Id}) enviado a {Email}", invitation.Id, recipientEmail);
    }

    /// <inheritdoc />
    public async Task SendPublicHelpRequestEmailAsync(PublicHelpRequestDto request, List<User> adminsToNotify)
    {
        if (!adminsToNotify.Any())
        {
            _logger.LogWarning(
                "Se recibió una solicitud de ayuda pública, pero no hay administradores para notificar.");
            return;
        }

        var htmlContent =
            await _razorRenderer.RenderViewToStringAsync("/Views/Shared/EmailTemplates/_PublicHelpRequestEmail.cshtml",
                request);

        foreach (var admin in adminsToNotify)
        {
            var subject = $"[Ayuda Pública] {request.Subject}";
            await _emailService.SendEmailAsync(admin.Email, $"{admin.FirstName} {admin.LastName}", subject,
                htmlContent);
        }
    }

    /// <inheritdoc />
    public async Task SendAuthenticatedHelpRequestEmailAsync(AuthenticatedHelpRequestDto request, User requestingUser,
        List<User> adminsToNotify)
    {
        if (!adminsToNotify.Any())
        {
            _logger.LogWarning("El usuario {UserId} solicitó ayuda, pero no hay administradores para notificar.",
                requestingUser.Id);
            return;
        }

        var emailModel = (Request: request, RequestingUser: requestingUser);
        var htmlContent =
            await _razorRenderer.RenderViewToStringAsync(
                "/Views/Shared/EmailTemplates/_AuthenticatedHelpRequestEmail.cshtml", emailModel);

        foreach (var admin in adminsToNotify)
        {
            var subject = $"[Ayuda Usuario] {request.Subject}";
            await _emailService.SendEmailAsync(admin.Email, $"{admin.FirstName} {admin.LastName}", subject,
                htmlContent);
        }
    }

    /// <inheritdoc />
    public async Task SendGenericAlertEmailAsync(string email, string name, GenericAlertViewModel model)
    {
        try
        {
            var emailHtml = await _razorRenderer.RenderViewToStringAsync(
                "~/Views/Shared/EmailTemplates/_GenericAlertEmail.cshtml",
                model);

            await _emailService.SendEmailAsync(email, name, model.Title, emailHtml);

            _logger.LogInformation("Alerta genérica '{Title}' enviada a {Email}", model.Title, email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar la alerta genérica a {Email}", email);
        }
    }

    /// <inheritdoc />
    public async Task SendAnomalyAlertEmailAsync(string recipientEmail, AnomalyAlertViewModel viewModel)
    {
        var htmlContent = await _razorRenderer.RenderViewToStringAsync(
            "/Views/Shared/EmailTemplates/_AnomalyAlertEmail.cshtml",
            viewModel
        );

        await _emailService.SendEmailAsync(recipientEmail, viewModel.UserName, "Alerta de Comportamiento Anómalo",
            htmlContent);
        _logger.LogInformation("Alerta de comportamiento anómalo enviada a {RecipientEmail}", recipientEmail);
    }

    /// <inheritdoc />
    public async Task SendMaskCreationAlertEmailAsync(string recipientEmail, MaskCreationAlertViewModel viewModel)
    {
        var htmlContent = await _razorRenderer.RenderViewToStringAsync(
            "/Views/Shared/EmailTemplates/_MaskCreationAlertEmail.cshtml",
            viewModel
        );

        await _emailService.SendEmailAsync(recipientEmail, viewModel.UserName,
            "Acción Requerida: Crear Máscaras Térmicas", htmlContent);
        _logger.LogInformation("Alerta de creación de máscara enviada a {RecipientEmail}", recipientEmail);
    }


    /// <inheritdoc />
    public async Task SendStressAlertEmailAsync(string recipientEmail, StressAlertViewModel viewModel)
    {
        var htmlContent = await _razorRenderer.RenderViewToStringAsync(
            "/Views/Shared/EmailTemplates/_StressAlertEmail.cshtml",
            viewModel
        );

        await _emailService.SendEmailAsync(recipientEmail, viewModel.UserName,
            $"Alerta de Estrés: {viewModel.PlantName}", htmlContent);
        _logger.LogInformation("Alerta de estrés para la planta {PlantName} enviada a {RecipientEmail}",
            viewModel.PlantName, recipientEmail);
    }

    /// <inheritdoc />
    public async Task SendReportByEmailAsync(string recipientEmail, string plantName, byte[] pdfAttachment)
    {
        var subject = $"Reporte de Estado Hídrico: {plantName}";
        var attachmentName = $"Reporte_{plantName.Replace(" ", "_")}_{DateTime.UtcNow.ToColombiaTime():yyyyMMdd}.pdf";

        var viewModel = new ReportByEmailViewModel
        {
            PlantName = plantName,
            GenerationDate = DateTime.UtcNow
        };

        var body = await _razorRenderer.RenderViewToStringAsync(
            "~/Views/Shared/EmailTemplates/_ReportByEmail.cshtml",
            viewModel);

        var recipientName = "Destinatario del Reporte";

        await _emailService.SendEmailWithAttachmentAsync(recipientEmail, recipientName, subject, body, pdfAttachment,
            attachmentName);
        _logger.LogInformation("Reporte en PDF para la planta {PlantName} enviado a {RecipientEmail}", plantName,
            recipientEmail);
    }

    /// <inheritdoc />
    public async Task SendInactivityWarningEmailAsync(User admin, int daysInactive, string loginUrl)
    {
        try
        {
            var title = "Aviso de Inactividad de Cuenta";
            var message = "";

            switch (daysInactive)
            {
                case 14:
                    title = "Primer Aviso de Inactividad de Cuenta";
                    message =
                        "Este es un recordatorio de que tu cuenta de administrador ha estado inactiva. Por favor, inicia sesión para mantenerla activa.";
                    break;
                case 22:
                    title = "Segundo Aviso de Inactividad de Cuenta";
                    message =
                        "Tu cuenta de administrador sigue inactiva. Para evitar que sea marcada para eliminación, por favor, inicia sesión pronto.";
                    break;
                case 30:
                    title = "Aviso Final: Cuenta Marcada para Eliminación por Inactividad";
                    message =
                        "Tu cuenta de administrador ha superado los 30 días de inactividad. A partir de ahora, otros administradores podrán eliminarla por razones de seguridad.";
                    break;
            }

            var viewModel = new AdminInactivityWarningViewModel
            {
                UserName = admin.FirstName,
                DaysInactive = daysInactive,
                WarningTitle = title,
                WarningMessage = message,
                LoginUrl = loginUrl
            };

            var body = await _razorRenderer.RenderViewToStringAsync(
                "~/Views/Shared/EmailTemplates/_AdminInactivityWarningEmail.cshtml",
                viewModel);

            await _emailService.SendEmailAsync(admin.Email, admin.FirstName, title, body);
            _logger.LogInformation("Aviso de inactividad de {Days} días enviado al administrador {Email}", daysInactive,
                admin.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar el correo de advertencia de inactividad al usuario {UserId}",
                admin.Id);
        }
    }

    /// <inheritdoc />
    public async Task SendAdminDeletionRequestEmailAsync(List<User> otherAdmins, string initiatingAdminName,
        string adminToDeleteName, string confirmationLink)
    {
        if (!otherAdmins.Any())
        {
            _logger.LogWarning(
                "Se inició una petición de eliminación para {AdminToDelete}, pero no hay otros administradores para notificar y confirmar.",
                adminToDeleteName);
            return;
        }

        _logger.LogInformation(
            "Enviando solicitud de segunda firma para eliminar a {AdminToDelete} a {Count} administradores.",
            adminToDeleteName, otherAdmins.Count);

        foreach (var admin in otherAdmins)
        {
            var viewModel = new AdminDeletionRequestViewModel
            {
                RecipientName = admin.FirstName,
                InitiatingAdminName = initiatingAdminName,
                AdminToDeleteName = adminToDeleteName,
                ConfirmationLink = confirmationLink
            };

            var body = await _razorRenderer.RenderViewToStringAsync(
                "~/Views/Shared/EmailTemplates/_AdminDeletionRequestEmail.cshtml",
                viewModel);

            await _emailService.SendEmailAsync(admin.Email, admin.FirstName, "Solicitud de Segunda Firma Requerida",
                body);
        }
    }

    /// <inheritdoc />
    public async Task SendAccountDeletedEmailAsync(string userEmail, string userName)
    {
        try
        {
            var viewModel = new AccountDeletedViewModel { UserName = userName };
            var body = await _razorRenderer.RenderViewToStringAsync(
                "~/Views/Shared/EmailTemplates/_AccountDeletedEmail.cshtml",
                viewModel);

            await _emailService.SendEmailAsync(userEmail, userName, "Tu cuenta ha sido eliminada", body);
            _logger.LogInformation("Notificación de eliminación de cuenta enviada a {Email}", userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar la notificación de eliminación de cuenta a {Email}", userEmail);
        }
    }
}