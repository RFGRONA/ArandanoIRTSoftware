namespace ArandanoIRT.Web._0_Domain.Entities;

public class AccountSettings
{
    /// <summary>
    ///     OPCIONAL (Todos los usuarios): Recibir alertas de estrés hídrico inicial.
    /// </summary>
    public bool EmailOnMildStressAlert { get; set; } = false;

    /// <summary>
    ///     OPCIONAL (Solo Admins): Recibir notificaciones del formulario de ayuda.
    /// </summary>
    public bool EmailOnHelpRequest { get; set; } = false;

    /// <summary>
    ///     OPCIONAL (Solo Admins): Recibir alertas por fallo de aplicación desde Grafana.
    /// </summary>
    public bool EmailOnAppFailureAlert { get; set; } = false;

    /// <summary>
    ///     OPCIONAL (Solo Admins): Recibir alertas por fallo de dispositivos desde Grafana.
    /// </summary>
    public bool EmailOnDeviceFailureAlert { get; set; } = false;

    /// <summary>
    ///     OPCIONAL (Solo Admins): Recibir alertas cuando un dispositivo deja de reportar datos.
    /// </summary>
    public bool EmailOnDeviceInactivity { get; set; } = false;
}