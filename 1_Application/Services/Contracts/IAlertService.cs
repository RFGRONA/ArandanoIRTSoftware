using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._3_Presentation.ViewModels.Alerts;
using ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para el servicio de alertas y notificaciones,
///     responsable de enviar todos los correos electrónicos de la aplicación.
/// </summary>
public interface IAlertService
{
    // --- Alertas de Seguridad ---
    /// <summary>
    ///     Envía una alerta de seguridad por correo electrónico cuando se detectan múltiples intentos fallidos de inicio de
    ///     sesión para un usuario.
    /// </summary>
    /// <param name="user">El usuario cuya cuenta está siendo objeto de intentos fallidos.</param>
    /// <param name="forgotPasswordUrl">La URL para que el usuario pueda restablecer su contraseña.</param>
    Task TriggerFailedLoginAlertAsync(User user, string forgotPasswordUrl);

    // --- Notificaciones de Cuenta ---
    /// <summary>
    ///     Envía un correo electrónico con un enlace para que el usuario pueda restablecer su contraseña.
    /// </summary>
    /// <param name="userEmail">El correo del destinatario.</param>
    /// <param name="userName">El nombre del destinatario.</param>
    /// <param name="resetLink">El enlace único para restablecer la contraseña.</param>
    Task SendPasswordResetEmailAsync(string userEmail, string userName, string resetLink);
    /// <summary>
    ///     Notifica al usuario que su contraseña ha sido cambiada exitosamente.
    /// </summary>
    /// <param name="userEmail">El correo del destinatario.</param>
    /// <param name="userName">El nombre del destinatario.</param>
    Task SendPasswordChangedEmailAsync(string userEmail, string userName);
    /// <summary>
    ///     Envía un correo de advertencia a un administrador sobre su inactividad prolongada.
    /// </summary>
    /// <param name="admin">El administrador inactivo.</param>
    /// <param name="daysInactive">El número de días que ha estado inactivo.</param>
    /// <param name="loginUrl">La URL para iniciar sesión.</param>
    Task SendInactivityWarningEmailAsync(User admin, int daysInactive, string loginUrl);
    /// <summary>
    ///     Informa a un usuario que su cuenta ha sido eliminada.
    /// </summary>
    /// <param name="userEmail">El correo del usuario eliminado.</param>
    /// <param name="userName">El nombre del usuario eliminado.</param>
    Task SendAccountDeletedEmailAsync(string userEmail, string userName);

    /// <summary>
    ///     Envía una solicitud de confirmación por correo a otros administradores para autorizar la eliminación de una cuenta
    ///     de administrador.
    /// </summary>
    /// <param name="otherAdmins">La lista de administradores que recibirán la notificación.</param>
    /// <param name="initiatingAdminName">El nombre del administrador que solicita la eliminación.</param>
    /// <param name="adminToDeleteName">El nombre del administrador cuya cuenta se solicita eliminar.</param>
    /// <param name="confirmationLink">El enlace para confirmar la eliminación.</param>
    Task SendAdminDeletionRequestEmailAsync(List<User> otherAdmins, string initiatingAdminName,
        string adminToDeleteName, string confirmationLink);

    // --- Notificaciones de Registro ---
    /// <summary>
    ///     Envía un correo de invitación para que un nuevo usuario se registre en la plataforma.
    /// </summary>
    /// <param name="recipientEmail">El correo del destinatario de la invitación.</param>
    /// <param name="recipientName">El nombre del destinatario.</param>
    /// <param name="invitation">El objeto de invitación que contiene el código.</param>
    Task SendInvitationEmailAsync(string recipientEmail, string recipientName, InvitationCode invitation);

    // --- Notificaciones de Soporte ---
    /// <summary>
    ///     Envía por correo electrónico una solicitud de ayuda realizada desde el formulario público (por un usuario no
    ///     autenticado).
    /// </summary>
    /// <param name="request">Los datos de la solicitud de ayuda.</param>
    /// <param name="adminsToNotify">La lista de administradores que deben ser notificados.</param>
    Task SendPublicHelpRequestEmailAsync(PublicHelpRequestDto request, List<User> adminsToNotify);

    /// <summary>
    ///     Envía por correo electrónico una solicitud de ayuda realizada por un usuario autenticado.
    /// </summary>
    /// <param name="request">Los datos de la solicitud de ayuda.</param>
    /// <param name="requestingUser">El usuario que realizó la solicitud.</param>
    /// <param name="adminsToNotify">La lista de administradores que deben ser notificados.</param>
    Task SendAuthenticatedHelpRequestEmailAsync(AuthenticatedHelpRequestDto request, User requestingUser,
        List<User> adminsToNotify);

    // --- Alerta Genérica ---
    /// <summary>
    ///     Envía una alerta genérica y personalizable por correo electrónico.
    /// </summary>
    /// <param name="email">El correo del destinatario.</param>
    /// <param name="name">El nombre del destinatario.</param>
    /// <param name="model">El modelo de vista con el contenido de la alerta.</param>
    Task SendGenericAlertEmailAsync(string email, string name, GenericAlertViewModel model);

    // --- Alertas de Análisis ---
    /// <summary>
    ///     Envía una alerta por correo sobre una anomalía detectada en los datos de una planta.
    /// </summary>
    /// <param name="recipientEmail">El correo del destinatario.</param>
    /// <param name="viewModel">El modelo de vista con los detalles de la anomalía.</param>
    Task SendAnomalyAlertEmailAsync(string recipientEmail, AnomalyAlertViewModel viewModel);
    /// <summary>
    ///     Envía una alerta por correo cuando se ha creado una nueva máscara térmica para una planta.
    /// </summary>
    /// <param name="recipientEmail">El correo del destinatario.</param>
    /// <param name="viewModel">El modelo de vista con los detalles de la alerta.</param>
    Task SendMaskCreationAlertEmailAsync(string recipientEmail, MaskCreationAlertViewModel viewModel);
    /// <summary>
    ///     Envía una alerta por correo electrónico sobre el estado de estrés hídrico de una planta.
    /// </summary>
    /// <param name="recipientEmail">El correo del destinatario.</param>
    /// <param name="viewModel">El modelo de vista con los detalles del estado de estrés.</param>
    Task SendStressAlertEmailAsync(string recipientEmail, StressAlertViewModel viewModel);
    /// <summary>
    ///     Envía un informe de planta en formato PDF como un archivo adjunto por correo electrónico.
    /// </summary>
    /// <param name="recipientEmail">El correo del destinatario.</param>
    /// <param name="plantName">El nombre de la planta para el asunto del correo.</param>
    /// <param name="pdfAttachment">El contenido del archivo PDF en bytes.</param>
    Task SendReportByEmailAsync(string recipientEmail, string plantName, byte[] pdfAttachment);
}