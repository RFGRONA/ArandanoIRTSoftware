using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace ArandanoIRT.Web._2_Infrastructure.Services;

/// <summary>
/// Un servicio en segundo plano que se ejecuta periódicamente para monitorear la inactividad de las cuentas de administrador.
/// Envía correos electrónicos de advertencia en intervalos predefinidos (14, 22 y 30 días) a los administradores inactivos.
/// </summary>
public class AdminInactivityService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminInactivityService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="AdminInactivityService"/>.
    /// </summary>
    /// <param name="scopeFactory">Factoría para crear nuevos ámbitos de inyección de dependencias, necesario en servicios singleton como BackgroundService.</param>
    /// <param name="configuration">La configuración de la aplicación para obtener valores como la URL base.</param>
    /// <param name="logger">El servicio de logging.</param>
    public AdminInactivityService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AdminInactivityService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Método principal del servicio en segundo plano. Contiene un bucle que se ejecuta durante toda la vida de la aplicación.
    /// </summary>
    /// <remarks>
    /// Utiliza un temporizador periódico que se activa cada hora. La lógica principal solo se ejecuta una vez al día,
    /// alrededor de las 7 AM (hora de Colombia), utilizando una entrada en la caché como un "lock" para evitar ejecuciones múltiples.
    /// </remarks>
    /// <param name="stoppingToken">Token que indica cuándo se debe detener el servicio.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de Inactividad de Administradores iniciado.");

        // El timer se ejecutará cada hora para verificar si es el momento de correr la tarea diaria.
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            if (DateTime.UtcNow.ToColombiaTime().Hour != 7) continue;

            await using var scope = _scopeFactory.CreateAsyncScope();
            var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

            // Usamos la caché para asegurarnos de que la tarea se ejecute solo una vez al día
            var cacheKey = $"AdminInactivityCheck_{DateTime.UtcNow:yyyy-MM-dd}";
            if (memoryCache.TryGetValue(cacheKey, out _)) continue; // La tarea para hoy ya se ejecutó.

            _logger.LogInformation("Ejecutando la revisión diaria de inactividad de administradores...");
            await CheckAdminsInactivityAsync(scope.ServiceProvider);

            // Marcamos la tarea de hoy como completada en la caché (expira en 23 horas)
            memoryCache.Set(cacheKey, true, TimeSpan.FromHours(23));
        }
    }

    /// <summary>
    /// Contiene la lógica principal para verificar la inactividad de los administradores.
    /// Obtiene todos los usuarios con rol de "Admin", calcula los días desde su último inicio de sesión y envía una notificación si se cumple alguna de las etapas (14, 22, 30 días).
    /// </summary>
    /// <param name="services">El proveedor de servicios del ámbito actual para resolver dependencias como UserManager y IAlertService.</param>
    private async Task CheckAdminsInactivityAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        var alertService = services.GetRequiredService<IAlertService>();

        var admins = await userManager.GetUsersInRoleAsync("Admin");
        var now = DateTime.UtcNow;

        // Construir la URL de login una sola vez
        var baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5239";
        var loginUrl = $"{baseUrl}/Account/Login";

        foreach (var admin in admins)
        {
            if (admin.LastLoginAt == null) continue; // Ignorar administradores que nunca han iniciado sesión

            var daysInactive = (int)Math.Floor((now - admin.LastLoginAt.Value).TotalDays);

            // Lógica de notificación por etapas exactas
            switch (daysInactive)
            {
                case 14:
                    _logger.LogInformation(
                        "El administrador {AdminId} ha alcanzado los 14 días de inactividad. Enviando primer aviso.",
                        admin.Id);
                    await alertService.SendInactivityWarningEmailAsync(admin, 14, loginUrl);
                    break;
                case 22:
                    _logger.LogInformation(
                        "El administrador {AdminId} ha alcanzado los 22 días de inactividad. Enviando segundo aviso.",
                        admin.Id);
                    await alertService.SendInactivityWarningEmailAsync(admin, 22, loginUrl);
                    break;
                case 30:
                    _logger.LogInformation(
                        "El administrador {AdminId} ha alcanzado los 30 días de inactividad. Enviando aviso final.",
                        admin.Id);
                    await alertService.SendInactivityWarningEmailAsync(admin, 30, loginUrl);
                    break;
            }
        }
    }
}