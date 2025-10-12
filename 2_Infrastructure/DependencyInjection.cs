using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._1_Application.Services.Implementation;
using ArandanoIRT.Web._2_Infrastructure.Authentication;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._2_Infrastructure.Services;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Polly;
using Polly.Extensions.Http;
using Serilog;

namespace ArandanoIRT.Web._2_Infrastructure;

/// <summary>
/// Proporciona métodos de extensión para configurar la inyección de dependencias
/// y registrar los servicios de la aplicación en el contenedor de IServiceCollection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra y configura todos los servicios relacionados con la capa de infraestructura.
    /// Esto incluye el contexto de la base de datos, la configuración de la aplicación (Settings),
    /// los clientes HTTP, los servicios de aplicación, los servicios de infraestructura y los trabajos en segundo plano.
    /// </summary>
    /// <param name="services">La colección de servicios para registrar.</param>
    /// <param name="configuration">La configuración de la aplicación.</param>
    /// <returns>La colección de servicios con los nuevos registros.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgresConnection");
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

        dataSourceBuilder.MapEnum<DeviceStatus>();
        dataSourceBuilder.MapEnum<ActivationStatus>();
        dataSourceBuilder.MapEnum<TokenStatus>();
        dataSourceBuilder.MapEnum<PlantStatus>();
        dataSourceBuilder.MapEnum<ExperimentalGroupType>();
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(dataSource, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .UseSnakeCaseNamingConvention());

        services.Configure<AdminCredentialsSettings>(configuration.GetSection(AdminCredentialsSettings.SectionName));
        services.Configure<WeatherApiSettings>(configuration.GetSection(WeatherApiSettings.SectionName));
        services.Configure<TokenSettings>(configuration.GetSection(TokenSettings.SectionName));
        services.Configure<MinioSettings>(configuration.GetSection(MinioSettings.SectionName));
        services.Configure<BrevoSettings>(configuration.GetSection(BrevoSettings.SectionName));
        services.Configure<AlertingSettings>(configuration.GetSection(AlertingSettings.SectionName));
        services.Configure<BackgroundJobSettings>(configuration.GetSection(BackgroundJobSettings.SectionName));
        services.Configure<AnalysisParametersSettings>(
            configuration.GetSection(AnalysisParametersSettings.SectionName));
        services.Configure<AnomalyParametersSettings>(configuration.GetSection(AnomalyParametersSettings.SectionName));
        services.Configure<CalibrationReminderSettings>(
            configuration.GetSection(CalibrationReminderSettings.SectionName));
        services.Configure<TurnstileSettings>(configuration.GetSection(TurnstileSettings.SectionName));
        services.Configure<ModelSettings>(configuration.GetSection(ModelSettings.SectionName));

        services.AddHttpClient("WeatherApi", (serviceProvider, client) =>
        {
            var weatherApiSettings = configuration.GetSection(WeatherApiSettings.SectionName).Get<WeatherApiSettings>();
            if (weatherApiSettings != null && !string.IsNullOrEmpty(weatherApiSettings.BaseUrl))
                client.BaseAddress = new Uri(weatherApiSettings.BaseUrl);
            else
                Log.Warning("BaseUrl for WeatherAPI is not configured.");
        });

        services.AddHttpClient<ITurnstileService, TurnstileService>()
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .CircuitBreakerAsync(
                        3,
                        TimeSpan.FromMinutes(3)
                    )
            );

        services.AddScoped<IWeatherService, WeatherService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IDataSubmissionService, DataSubmissionService>();
        services.AddScoped<ICropService, CropService>();
        services.AddScoped<IPlantService, PlantService>();
        services.AddScoped<IDeviceAdminService, DeviceAdminService>();
        services.AddScoped<IDataQueryService, DataQueryService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<ISupportService, SupportService>();
        services.AddScoped<IObservationService, ObservationService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IAlertTriggerService, AlertTriggerService>();
        services.AddScoped<IEnvironmentalDataProvider, EnvironmentalDataProvider>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();
        services.AddScoped<ITurnstileService, TurnstileService>();
        services.AddScoped<IAnalysisExecutionService, AnalysisExecutionService>();

        services.AddScoped<IFileStorageService, MinioStorageService>();
        services.AddScoped<IEmailService, BrevoEmailService>();
        services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
        services.AddMemoryCache();

        services.AddHostedService<DeviceInactivityService>();
        services.AddHostedService<WaterStressAnalysisService>();
        services.AddHostedService<DailyTasksService>();
        services.AddHostedService<AdminInactivityService>();

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(configuration.GetConnectionString("PostgresConnection"))));

        services.AddHangfireServer(options => { options.WorkerCount = 2; });

        services.AddSingleton<IConditionPredictor>(serviceProvider =>
        {
            var modelSettings = serviceProvider.GetRequiredService<IOptions<ModelSettings>>().Value;
            var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            var modelPath = Path.Combine(environment.ContentRootPath, modelSettings.OnnxModelPath);

            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"El archivo del modelo ONNX no se encontró en la ruta: {modelPath}");

            return new OnnxConditionPredictor(modelPath);
        });

        return services;
    }

    /// <summary>
    /// Configura y registra los servicios de autenticación y autorización personalizados para la aplicación.
    /// Incluye la configuración de ASP.NET Core Identity, el manejo de cookies de sesión y
    /// un esquema de autenticación específico para dispositivos.
    /// </summary>
    /// <param name="services">La colección de servicios para registrar.</param>
    /// <returns>La colección de servicios con los nuevos registros.</returns>
    public static IServiceCollection AddCustomAuthentication(this IServiceCollection services)
    {
        // 1. Sistema principal de usuarios, roles y contraseñas.
        services.AddIdentity<User, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.User.RequireUniqueEmail = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<SecurityStampValidatorOptions>(options => { options.ValidationInterval = TimeSpan.Zero; });

        // 2. Autenticación y Cookies
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";

            // Duración fija de la sesión de 4 horas.
            options.ExpireTimeSpan = TimeSpan.FromHours(4);

            // Desactiva la expiración deslizante para que la sesión termine 4 horas después del login, sin importar la actividad.
            options.SlidingExpiration = false;

            // Activa la validación del SecurityStamp.
            // Fuerza a la aplicación a verificar en cada petición si la sesión sigue siendo válida (p. ej. si la contraseña cambió).
            options.Events.OnValidatePrincipal = async context =>
            {
                var principal = context.Principal;
                if (principal != null)
                    // Si la cookie pertenece al usuario bootstrap (por nombre o rol), no se valida el security stamp.
                    if (principal.IsInRole("BootstrapAdmin") || principal.Identity?.Name == "ROOT_BOOTSTRAP_USER")
                        return;

                await SecurityStampValidator.ValidatePrincipalAsync(context);
            };
        });

        // Esquema personalizado para dispositivos, es independiente.
        services.AddAuthentication()
            .AddScheme<DeviceAuthenticationOptions, DeviceAuthenticationHandler>(
                DeviceAuthenticationOptions.DefaultScheme, options => { });

        // 3. Autorización
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("DeviceAuthenticated", policy =>
            {
                policy.AddAuthenticationSchemes(DeviceAuthenticationOptions.DefaultScheme);
                policy.RequireAuthenticatedUser();
            });
        });

        return services;
    }

    /// <summary>
    /// Registra los servicios necesarios para la capa de presentación (MVC).
    /// Esto incluye el acceso al HttpContext y la configuración de controladores y vistas.
    /// </summary>
    /// <param name="services">La colección de servicios para registrar.</param>
    /// <returns>La colección de servicios con los nuevos registros.</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddControllersWithViews()
            .AddRazorOptions(options =>
            {
                options.AreaViewLocationFormats.Clear();
                options.AreaViewLocationFormats.Add("/Views/{2}/{1}/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Views/{2}/Shared/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
            });

        return services;
    }
}