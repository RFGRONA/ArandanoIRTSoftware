// En _1_Application/Services/Contracts/IAlertTriggerService.cs

using ArandanoIRT.Web._1_Application.DTOs.Alerts;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IAlertTriggerService
{
    Task ProcessGrafanaWebhookAsync(GrafanaWebhookPayload payload);
    Task CheckDeviceInactivityAsync();
    Task TriggerStressAlertsAsync();
    Task SendGroupedAlertSummaryAsync(string alertType, AlertGroupState group);
    Task TriggerAnomalyAlertAsync(int plantId, string plantName);
    Task TriggerMaskCreationAlertAsync(List<string> plantNames);
}