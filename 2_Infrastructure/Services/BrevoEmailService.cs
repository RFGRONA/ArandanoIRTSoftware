using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using brevo_csharp.Api;
using brevo_csharp.Model;
using Microsoft.Extensions.Options;
using Configuration = brevo_csharp.Client.Configuration;

namespace ArandanoIRT.Web._2_Infrastructure.Services;

public class BrevoEmailService : IEmailService
{
    private readonly BrevoSettings _brevoSettings;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(IOptions<BrevoSettings> brevoSettings, ILogger<BrevoEmailService> logger)
    {
        _brevoSettings = brevoSettings.Value;
        _logger = logger;
    }

    public async Task<Result> SendEmailAsync(string toEmail, string toName, string subject, string htmlContent)
    {
        if (string.IsNullOrEmpty(_brevoSettings.ApiKey))
        {
            _logger.LogError("Brevo API Key no está configurada.");
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
            var sendSmtpEmail = new SendSmtpEmail(sender, to, null, null, htmlContent, null, subject);
            var result = await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation("Correo enviado exitosamente a {ToEmail}. MessageId: {MessageId}", toEmail,
                result.MessageId);
            return Result.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al enviar correo a {ToEmail} vía Brevo", toEmail);
            return Result.Failure($"Error al enviar correo: {e.Message}");
        }
    }
}