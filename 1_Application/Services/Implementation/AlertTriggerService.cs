using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Alerts;
using ArandanoIRT.Web._1_Application.DTOs.Analysis;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._3_Presentation.ViewModels;
using Microsoft.Extensions.Caching.Memory;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class AlertTriggerService : IAlertTriggerService
{
    private readonly IAlertService _alertService;
    private readonly IConfiguration _configuration;
    private readonly IDeviceService _deviceService;
    private readonly ILogger<AlertTriggerService> _logger;
    private readonly IUserService _userService;
    private readonly IMemoryCache _memoryCache;

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

    public async Task ProcessGrafanaWebhookAsync(GrafanaWebhookPayload payload)
    {
        // 1. Extraemos la etiqueta personalizada que define el tipo de alerta
        if (!payload.CommonLabels.TryGetValue("alert_type", out var alertType) || string.IsNullOrEmpty(alertType))
        {
            _logger.LogWarning("Alerta de Grafana recibida sin la etiqueta 'alert_type'.");
            return;
        }

        var cacheKey = $"grafana_alert_group_{alertType}";
    
        // 2. "Traducimos" el tipo de alerta a un resumen en español
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

        // 3. Buscamos un grupo de alertas existente en la caché
        if (!_memoryCache.TryGetValue(cacheKey, out AlertGroupState alertGroup))
        {
            // Si no existe, creamos uno nuevo
            alertGroup = new AlertGroupState { Summary = summary };
            _logger.LogInformation("Creando nuevo grupo de alertas para: {AlertType}", alertType);
        }
        else
        {
            // Si ya existe, solo incrementamos el contador
            alertGroup.Count++;
        }

        // 4. Guardamos o actualizamos el grupo en la caché con una expiración de 1 hora
        _memoryCache.Set(cacheKey, alertGroup, TimeSpan.FromHours(1));
        _logger.LogInformation("Grupo de alertas '{AlertType}' actualizado. Conteo actual: {Count}", alertType, alertGroup.Count);
    }


    public async Task CheckDeviceInactivityAsync()
    {
        var inactivityMultiplier = _configuration.GetValue("Alerting:InactivityCheckMultiplier", 4);
        var inactiveDevices = await _deviceService.GetInactiveDevicesAsync(inactivityMultiplier);

        if (!inactiveDevices.Any()) return;

        var adminsToNotify = await _userService.GetAdminsToNotifyAsync(s => s.EmailOnDeviceInactivity);

        if (!adminsToNotify.Any()) return;

        foreach (var device in inactiveDevices)
        {
            _logger.LogWarning("Dispositivo inactivo detectado: {DeviceName}", device.Name);

            // Cambiamos el estado del dispositivo
            await _deviceService.UpdateDeviceStatusAsync(device.Id, DeviceStatus.INACTIVE);

            var viewModel = new GenericAlertViewModel
            {
                Title = "Alerta de Inactividad de Dispositivo",
                Message =
                    $"El dispositivo '{device.Name}' (MAC: {device.MacAddress}) no ha reportado datos en el tiempo esperado.",
                Severity = "Warning"
            };

            foreach (var admin in adminsToNotify)
                await _alertService.SendGenericAlertEmailAsync(admin.Email, admin.FirstName, viewModel);
        }
    }
    
    public async Task SendGroupedAlertSummaryAsync(string alertType, AlertGroupState group)
    {
        // 1. Obtenemos la lista de administradores que deben ser notificados para este tipo de alerta
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
                return; // Tipo no reconocido, no hacemos nada
        }

        if (!recipients.Any()) return;

        // 2. Creamos el ViewModel con los textos en español
        var viewModel = new GenericAlertViewModel
        {
            Title = title,
            Message = $"Se han detectado {group.Count} alerta(s) de '{group.Summary}' en la última hora. Por favor, revise los logs del sistema para más detalles.",
            Severity = "Critical" // O podrías pasarlo desde Grafana también
        };
    
        // 3. Enviamos el correo a cada destinatario
        foreach (var admin in recipients)
        {
            await _alertService.SendGenericAlertEmailAsync(admin.Email, admin.FirstName, viewModel);
        }

        _logger.LogInformation("Resumen de alertas para '{AlertType}' enviado a {RecipientCount} administradores.", alertType, recipients.Count);
    }

    public Task TriggerStressAlertsAsync()
    {
        _logger.LogInformation("Este método se implementará en la Fase 4.");
        return Task.CompletedTask;
    }
    
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
                PlantId = plantId
            };
            await _alertService.SendAnomalyAlertEmailAsync(user.Email, viewModel);
        }
        _logger.LogWarning("Alerta de comportamiento anómalo enviada para la planta {PlantName}", plantName);
    }

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
}