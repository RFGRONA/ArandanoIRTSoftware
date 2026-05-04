namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Define las configuraciones de notificación y alertas para la cuenta de un usuario.
/// </summary>
public class AccountSettings
{
    /// <summary>
    /// Indica si el usuario desea recibir alertas por correo electrónico cuando se detecta estrés hídrico incipiente.
    /// Aplica a todos los usuarios.
    /// </summary>
    public bool EmailOnMildStressAlert { get; set; } = false;

    /// <summary>
    /// Indica si el usuario (administrador) desea recibir notificaciones por correo electrónico
    /// cuando un cliente envía una solicitud a través del formulario de ayuda.
    /// </summary>
    public bool EmailOnHelpRequest { get; set; } = false;

    /// <summary>
    /// Indica si el usuario (administrador) desea recibir alertas por correo electrónico
    /// sobre fallos de la aplicación, generalmente provenientes de un sistema de monitoreo como Grafana.
    /// </summary>
    public bool EmailOnAppFailureAlert { get; set; } = false;

    /// <summary>
    /// Indica si el usuario (administrador) desea recibir alertas por correo electrónico
    /// sobre fallos en los dispositivos, generalmente provenientes de un sistema de monitoreo como Grafana.
    /// </summary>
    public bool EmailOnDeviceFailureAlert { get; set; } = false;

    /// <summary>
    /// Indica si el usuario (administrador) desea recibir alertas por correo electrónico
    /// cuando un dispositivo deja de reportar datos por un período prolongado.
    /// </summary>
    public bool EmailOnDeviceInactivity { get; set; } = false;
}