using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using brevo_csharp.Api;
using brevo_csharp.Model;
using Microsoft.Extensions.Options;
using Configuration = brevo_csharp.Client.Configuration;

namespace ArandanoIRT.Web._2_Infrastructure.Services;

/// <summary>
/// Implementación del servicio de envío de correos que utiliza la API de Brevo (antes Sendinblue).
/// </summary>
public class BrevoEmailService : IEmailService
{
    private readonly BrevoSettings _brevoSettings;
    private readonly ILogger<BrevoEmailService> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="BrevoEmailService"/>.
    /// </summary>
    /// <param name="brevoSettings">La configuración específica de Brevo, como la API Key.</param>
    /// <param name="logger">El servicio de logging.</param>
    public BrevoEmailService(IOptions<BrevoSettings> brevoSettings, ILogger<BrevoEmailService> logger)
    {
        _brevoSettings = brevoSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> SendEmailAsync(string toEmail, string toName, string subject, string htmlContent)
    {
        return await SendEmailInternalAsync(toEmail, toName, subject, htmlContent, null);
    }

    /// <inheritdoc />
    public async Task<Result> SendEmailWithAttachmentAsync(string toEmail, string toName, string subject, string htmlContent, byte[] attachmentContent, string attachmentName)
    {
        var attachment = new SendSmtpEmailAttachment(content: attachmentContent, name: attachmentName);
        return await SendEmailInternalAsync(toEmail, toName, subject, htmlContent, new List<SendSmtpEmailAttachment> { attachment });
    }

    /// <summary>
    /// Método central que construye y envía el correo electrónico utilizando el SDK de Brevo.
    /// </summary>
    /// <param name="toEmail">La dirección de correo del destinatario.</param>
    /// <param name="toName">El nombre del destinatario.</param>
    /// <param name="subject">El asunto del correo.</param>
    /// <param name="htmlContent">El cuerpo del correo en formato HTML.</param>
    /// <param name="attachments">Una lista opcional de archivos adjuntos.</param>
    /// <returns>Un objeto Result que indica si el envío fue exitoso.</returns>
    private async Task<Result> SendEmailInternalAsync(string toEmail, string toName, string subject, string htmlContent, List<SendSmtpEmailAttachment>? attachments)
    {
        if (string.IsNullOrEmpty(_brevoSettings.ApiKey))
        {
            _logger.LogError("La API Key de Brevo no está configurada.");
            return Result.Failure("El servicio de correo no está configurado.");
        }

        Configuration.Default.ApiKey["api-key"] = _brevoSettings.ApiKey;

        var apiInstance = new TransactionalEmailsApi();
        var senderName = "AIRT Info";
        var senderEmail = "info@arandanoirt.co";
        var sender = new SendSmtpEmailSender(senderName, senderEmail);
        var to = new List<SendSmtpEmailTo> { new(toEmail, toName) };

        try
        {
            var sendSmtpEmail = new SendSmtpEmail(
                sender: sender,
                to: to,
                htmlContent: htmlContent,
                subject: subject,
                attachment: attachments
            );

            var result = await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation("Correo enviado exitosamente a {ToEmail}. MessageId: {MessageId}", toEmail, result.MessageId);
            return Result.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al enviar correo a {ToEmail} vía Brevo", toEmail);
            return Result.Failure($"Error al enviar correo: {e.Message}");
        }
    }
}