using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Alerts;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.ViewModels.Alerts;
using ArandanoIRT.Web._3_Presentation.ViewModels.Analysis;
using Microsoft.Extensions.Caching.Memory;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

/// <summary>
///     Implementación del servicio que gestiona la lógica para disparar alertas.
///     Utiliza un sistema de caché en memoria para agrupar alertas repetitivas y evitar el envío masivo de notificaciones.
/// </summary>
public class AlertTriggerService : IAlertTriggerService
{
    private readonly IAlertService _alertService;
    private readonly IConfiguration _configuration;
    private readonly IDeviceService _deviceService;
    private readonly ILogger<AlertTriggerService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IUserService _userService;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="AlertTriggerService" />.
    /// </summary>
    public AlertTriggerService(
        ILogger<AlertTriggerService> logger,
        IDeviceService deviceService,
        IAlertService alertService,
        IUserService userService,
        IMemoryCache memoryCache,
        IConfiguration configuration)
    {
        _logger = logger;
        _deviceService = deviceService;
        _alertService = alertService;
        _userService = userService;
        _configuration = configuration;
        _memoryCache = memoryCache;
    }

    /// <inheritdoc />
    public async Task ProcessGrafanaWebhookAsync(GrafanaWebhookPayload payload)
    {
        if (!payload.CommonLabels.TryGetValue("alert_type", out var alertType) || string.IsNullOrEmpty(alertType))
        {
            _logger.LogWarning("Alerta de Grafana recibida sin la etiqueta 'alert_type'.");
            return;
        }

        var cacheKey = $"grafana_alert_group_{alertType}";

        string summary;
        switch (alertType)
        {
            case "device_failure":
                summary = "Fallo de Dispositivo";
                break;
            case "application_failure":
                summary = "Fallo de Aplicación";
                break;
            default:
                _logger.LogWarning("Tipo de alerta no reconocido: {AlertType}", alertType);
                return;
        }

        if (!_memoryCache.TryGetValue(cacheKey, out AlertGroupState alertGroup))
        {
            alertGroup = new AlertGroupState { Summary = summary };
            _logger.LogInformation("Creando nuevo grupo de alertas para: {AlertType}", alertType);
        }
        else
        {
            alertGroup.Count++;
        }

        _memoryCache.Set(cacheKey, alertGroup, TimeSpan.FromHours(1));
        _logger.LogInformation("Grupo de alertas '{AlertType}' actualizado. Conteo actual: {Count}", alertType,
            alertGroup.Count);
    }

    /// <inheritdoc />
    public async Task CheckDeviceInactivityAsync()
    {
        var inactivityMultiplier = _configuration.GetValue("Alerting:InactivityCheckMultiplier", 4);
        var inactiveDevices = await _deviceService.GetInactiveDevicesAsync(inactivityMultiplier);

        if (!inactiveDevices.Any()) return;

        var adminsToNotify = await _userService.GetAdminsToNotifyAsync(s => s.EmailOnDeviceInactivity);

        if (!adminsToNotify.Any()) return;

        foreach (var device in inactiveDevices)
            if (device.Status != DeviceStatus.INACTIVE)
            {
                _logger.LogWarning("Dispositivo inactivo detectado: {DeviceName}", device.Name);

                await _deviceService.UpdateDeviceStatusAsync(device.Id, DeviceStatus.INACTIVE);

                var viewModel = new GenericAlertViewModel
                {
                    Title = "Alerta de Inactividad de Dispositivo",
                    Message =
                        $"El dispositivo '{device.Name}' (MAC: {device.MacAddress}) no ha reportado datos en el tiempo esperado.",
                    Severity = "Precaución",
                    AlertTime = DateTime.UtcNow
                };

                foreach (var admin in adminsToNotify)
                    await _alertService.SendGenericAlertEmailAsync(admin.Email, admin.FirstName, viewModel);
            }
    }

    /// <inheritdoc />
    public async Task SendGroupedAlertSummaryAsync(string alertType, AlertGroupState group)
    {
        List<User> recipients;
        string title;

        switch (alertType)
        {
            case "device_failure":
                recipients = await _userService.GetAdminsToNotifyAsync(s => s.EmailOnDeviceFailureAlert);
                title = "Resumen de Alertas: Fallo de Dispositivos";
                break;
            case "application_failure":
                recipients = await _userService.GetAdminsToNotifyAsync(s => s.EmailOnAppFailureAlert);
                title = "Resumen de Alertas: Fallo de Aplicación";
                break;
            default:
                return;
        }

        if (!recipients.Any()) return;

        var viewModel = new GenericAlertViewModel
        {
            Title = title,
            Message =
                $"Se han detectado {group.Count} alerta(s) de '{group.Summary}' en la última hora. Por favor, revise los logs del sistema para más detalles.",
            Severity = "Critico",
            AlertTime = DateTime.UtcNow
        };

        foreach (var admin in recipients)
            await _alertService.SendGenericAlertEmailAsync(admin.Email, admin.FirstName, viewModel);

        _logger.LogInformation("Resumen de alertas para '{AlertType}' enviado a {RecipientCount} administradores.",
            alertType, recipients.Count);
    }

    /// <inheritdoc />
    public async Task TriggerAnomalyAlertAsync(int plantId, string plantName)
    {
        var usersToNotify = await _userService.GetAllUsersAsync();
        if (!usersToNotify.Any()) return;

        foreach (var user in usersToNotify)
        {
            var viewModel = new AnomalyAlertViewModel
            {
                UserName = user.FirstName,
                PlantName = plantName,
                PlantId = plantId,
                AlertTime = DateTime.UtcNow
            };
            await _alertService.SendAnomalyAlertEmailAsync(user.Email, viewModel);
        }

        _logger.LogWarning("Alerta de comportamiento anómalo enviada para la planta {PlantName}", plantName);
    }

    /// <inheritdoc />
    public async Task TriggerMaskCreationAlertAsync(List<string> plantNames)
    {
        if (!plantNames.Any()) return;

        var usersToNotify = await _userService.GetAllUsersAsync();
        if (!usersToNotify.Any()) return;

        foreach (var user in usersToNotify)
        {
            var viewModel = new MaskCreationAlertViewModel
            {
                UserName = user.FirstName,
                PlantNames = plantNames
            };
            await _alertService.SendMaskCreationAlertEmailAsync(user.Email, viewModel);
        }

        _logger.LogInformation("Alerta de creación de máscara enviada para {Count} plantas.", plantNames.Count);
    }

    /// <inheritdoc />
    public async Task TriggerStressAlertAsync(int plantId, string plantName, PlantStatus newStatus,
        PlantStatus previousStatus, float cwsiValue)
    {
        var usersToNotify = await _userService.GetAllUsersAsync();
        if (!usersToNotify.Any()) return;

        var baseUrl = _configuration["BaseUrl"];
        var analysisUrl = $"{baseUrl}/Admin/Analytics/Details/{plantId}";

        foreach (var user in usersToNotify)
        {
            var viewModel = new StressAlertViewModel
            {
                UserName = user.FirstName,
                PlantName = plantName,
                NewStatus = newStatus.ToString().Replace("_", " "),
                PreviousStatus = previousStatus.ToString().Replace("_", " "),
                CwsiValue = cwsiValue,
                CtaButtonUrl = analysisUrl
            };
            await _alertService.SendStressAlertEmailAsync(user.Email, viewModel);
        }

        _logger.LogInformation(
            "Disparando alerta de cambio de estado para la planta {PlantName}. Nuevo estado: {NewStatus}",
            plantName, newStatus);
    }
}