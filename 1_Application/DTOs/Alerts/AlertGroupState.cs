namespace ArandanoIRT.Web._1_Application.DTOs.Alerts;

/// <summary>
/// Mantiene el estado de un grupo de alertas similares para evitar el envío de notificaciones repetitivas.
/// </summary>
public class AlertGroupState
{
    /// <summary>
    /// Contador de cuántas veces ha ocurrido la alerta en el grupo.
    /// </summary>
    public int Count { get; set; } = 1;

    /// <summary>
    /// Marca de tiempo de cuándo se recibió la primera alerta del grupo.
    /// </summary>
    public DateTime FirstAlertTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Resumen o mensaje de la alerta.
    /// </summary>
    public string Summary { get; set; }
}