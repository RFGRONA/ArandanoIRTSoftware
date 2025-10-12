using ArandanoIRT.Web._0_Domain.Common;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para un servicio de bajo nivel encargado de enviar correos electrónicos.
///     Esta interfaz abstrae la implementación específica del proveedor de correo (ej. Brevo, SendGrid, SMTP).
/// </summary>
public interface IEmailService
{
    /// <summary>
    ///     Envía un correo electrónico con contenido HTML.
    /// </summary>
    /// <param name="toEmail">La dirección de correo del destinatario.</param>
    /// <param name="toName">El nombre del destinatario.</param>
    /// <param name="subject">El asunto del correo.</param>
    /// <param name="htmlContent">El cuerpo del correo en formato HTML.</param>
    /// <returns>Un objeto <c>Result</c> que indica si el envío fue exitoso.</returns>
    Task<Result> SendEmailAsync(string toEmail, string toName, string subject, string htmlContent);

    /// <summary>
    ///     Envía un correo electrónico con contenido HTML y un archivo adjunto.
    /// </summary>
    /// <param name="toEmail">La dirección de correo del destinatario.</param>
    /// <param name="toName">El nombre del destinatario.</param>
    /// <param name="subject">El asunto del correo.</param>
    /// <param name="htmlContent">El cuerpo del correo en formato HTML.</param>
    /// <param name="attachmentContent">El contenido del archivo adjunto como un arreglo de bytes.</param>
    /// <param name="attachmentName">El nombre del archivo adjunto (incluyendo extensión).</param>
    /// <returns>Un objeto <c>Result</c> que indica si el envío fue exitoso.</returns>
    Task<Result> SendEmailWithAttachmentAsync(string toEmail, string toName, string subject, string htmlContent,
        byte[] attachmentContent, string attachmentName);
}