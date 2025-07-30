using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.ViewModels;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class AlertService : IAlertService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<AlertService> _logger;
    private readonly IRazorViewToStringRenderer _razorRenderer;

    public AlertService(
        IEmailService emailService,
        IRazorViewToStringRenderer razorRenderer,
        ILogger<AlertService> logger)
    {
        _emailService = emailService;
        _razorRenderer = razorRenderer;
        _logger = logger;
    }

    // --- Alertas de Seguridad ---
    public async Task TriggerFailedLoginAlertAsync(User user)
    {
        try
        {
            var viewModel = new FailedLoginAlertViewModel
            {
                UserName = user.FirstName,
                AlertTime = DateTime.UtcNow
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

    // --- Notificaciones de Cuenta ---
    public async Task SendPasswordResetEmailAsync(string userEmail, string userName, string resetLink)
    {
        var emailModel = (Name: userName, ResetLink: resetLink);
        var emailHtml =
            await _razorRenderer.RenderViewToStringAsync("/Views/Shared/EmailTemplates/_ForgotPasswordEmail.cshtml",
                emailModel);
        await _emailService.SendEmailAsync(userEmail, userName, "Restablece tu contraseña", emailHtml);
    }

    public async Task SendPasswordChangedEmailAsync(string userEmail, string userName)
    {
        var emailHtml =
            await _razorRenderer.RenderViewToStringAsync("/Views/Shared/EmailTemplates/_PasswordChangedEmail.cshtml",
                userName);
        await _emailService.SendEmailAsync(userEmail, userName, "Tu Contraseña ha sido Cambiada", emailHtml);
    }

    // --- Notificaciones de Registro ---
    public async Task SendInvitationEmailAsync(string recipientEmail, string recipientName, InvitationCode invitation)
    {
        var emailHtml =
            await _razorRenderer.RenderViewToStringAsync("/Views/Shared/EmailTemplates/_InvitationEmail.cshtml",
                invitation);
        await _emailService.SendEmailAsync(recipientEmail, recipientName, "Invitación a Arandano IRT", emailHtml);
        _logger.LogInformation("Correo de invitación (ID: {Id}) enviado a {Email}", invitation.Id, recipientEmail);
    }

    // --- Notificaciones de Soporte ---
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
    
    public async Task SendAnomalyAlertEmailAsync(string recipientEmail, AnomalyAlertViewModel viewModel)
    {
        string htmlContent = await _razorRenderer.RenderViewToStringAsync(
            "/Views/Shared/EmailTemplates/_AnomalyAlertEmail.cshtml", 
            viewModel
        );

        await _emailService.SendEmailAsync(recipientEmail, viewModel.UserName, "Alerta de Comportamiento Anómalo", htmlContent);
        _logger.LogInformation("Alerta de comportamiento anómalo enviada a {RecipientEmail}", recipientEmail);
        
    }

    public async Task SendMaskCreationAlertEmailAsync(string recipientEmail, MaskCreationAlertViewModel viewModel)
    {
        string htmlContent = await _razorRenderer.RenderViewToStringAsync(
            "/Views/Shared/EmailTemplates/_MaskCreationAlertEmail.cshtml", 
            viewModel
        );
        
        await _emailService.SendEmailAsync(recipientEmail, viewModel.UserName, "Acción Requerida: Crear Máscaras Térmicas", htmlContent);
        _logger.LogInformation("Alerta de creación de máscara enviada a {RecipientEmail}", recipientEmail);
    }
}