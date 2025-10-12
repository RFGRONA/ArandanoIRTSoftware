using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Alerts;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para el servicio que gestiona la lógica de negocio para disparar alertas.
///     Este servicio decide cuándo una alerta debe ser enviada y agrupa notificaciones para evitar el spam.
/// </summary>
public interface IAlertTriggerService
{
    /// <summary>
    ///     Procesa una alerta entrante recibida desde un webhook de Grafana.
    /// </summary>
    /// <param name="payload">Los datos de la alerta enviados por Grafana.</param>
    Task ProcessGrafanaWebhookAsync(GrafanaWebhookPayload payload);

    /// <summary>
    ///     Ejecuta una verificación periódica para detectar dispositivos que han dejado de enviar datos y dispara las alertas
    ///     correspondientes.
    /// </summary>
    Task CheckDeviceInactivityAsync();

    /// <summary>
    ///     Envía un correo electrónico de resumen para un grupo de alertas similares que han ocurrido en un período de tiempo.
    /// </summary>
    /// <param name="alertType">El tipo de alerta que se está resumiendo (ej. "Fallo de Aplicación").</param>
    /// <param name="group">El estado del grupo de alertas, que contiene el conteo y el resumen.</param>
    Task SendGroupedAlertSummaryAsync(string alertType, AlertGroupState group);

    /// <summary>
    ///     Dispara una alerta de anomalía de datos para una planta específica.
    /// </summary>
    /// <param name="plantId">El ID de la planta afectada.</param>
    /// <param name="plantName">El nombre de la planta afectada.</param>
    Task TriggerAnomalyAlertAsync(int plantId, string plantName);

    /// <summary>
    ///     Dispara una notificación informando que se han creado nuevas máscaras térmicas.
    /// </summary>
    /// <param name="plantNames">La lista de nombres de las plantas para las cuales se crearon máscaras.</param>
    Task TriggerMaskCreationAlertAsync(List<string> plantNames);

    /// <summary>
    ///     Dispara una alerta cuando el estado de estrés hídrico de una planta cambia.
    /// </summary>
    /// <param name="plantId">El ID de la planta cuyo estado cambió.</param>
    /// <param name="plantName">El nombre de la planta.</param>
    /// <param name="newStatus">El nuevo estado de estrés de la planta.</param>
    /// <param name="previousStatus">El estado de estrés anterior de la planta.</param>
    /// <param name="cwsiValue">El valor del CWSI que provocó el cambio de estado.</param>
    Task TriggerStressAlertAsync(int plantId, string plantName, PlantStatus newStatus, PlantStatus previousStatus,
        float cwsiValue);
}