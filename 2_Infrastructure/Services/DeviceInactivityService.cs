using ArandanoIRT.Web._1_Application.DTOs.Alerts;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ArandanoIRT.Web._2_Infrastructure.Services;

public class DeviceInactivityService : BackgroundService
{
    private readonly ILogger<DeviceInactivityService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundJobSettings _settings;

    public DeviceInactivityService(
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundJobSettings> settings,
        ILogger<DeviceInactivityService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_settings.InactivityCheckIntervalMinutes);
        using var timer = new PeriodicTimer(interval);

        _logger.LogInformation("Servicio de inactividad iniciado. Verificando cada {Minutes} minutos.",
            _settings.InactivityCheckIntervalMinutes);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var alertTriggerService = scope.ServiceProvider.GetRequiredService<IAlertTriggerService>();
            var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

            // Tarea 1: Comprobar inactividad (como antes)
            await alertTriggerService.CheckDeviceInactivityAsync();

            // Tarea 2: Comprobar grupos de alertas de Grafana
            var alertTypes = new[] { "device_failure", "application_failure" };
            foreach (var type in alertTypes)
            {
                var cacheKey = $"grafana_alert_group_{type}";
                if (memoryCache.TryGetValue(cacheKey, out AlertGroupState group))
                {
                    // Si ha pasado más de una hora, enviamos el resumen
                    if (DateTime.UtcNow - group.FirstAlertTimestamp > TimeSpan.FromHours(1))
                    {
                        await alertTriggerService.SendGroupedAlertSummaryAsync(type, group);
                        memoryCache.Remove(cacheKey); // Limpiamos la caché
                    }
                }
            }
        }
    }
}