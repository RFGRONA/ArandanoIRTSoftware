using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._1_Application.Services.Implementation;
using ArandanoIRT.Web._2_Infrastructure.Authentication;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._2_Infrastructure.Services;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;

namespace ArandanoIRT.Web._2_Infrastructure;

/// <summary>
///     Extension methods for setting up services in the IServiceCollection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    ///     Adds infrastructure services to the container.
    ///     This includes database, application services, and external service clients.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database Context
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

        // Configuration Settings
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

        // HTTP Client for Weather API
        services.AddHttpClient("WeatherApi", (serviceProvider, client) =>
        {
            var weatherApiSettings = configuration.GetSection(WeatherApiSettings.SectionName).Get<WeatherApiSettings>();
            if (weatherApiSettings != null && !string.IsNullOrEmpty(weatherApiSettings.BaseUrl))
                client.BaseAddress = new Uri(weatherApiSettings.BaseUrl);
            else
                Log.Warning("BaseUrl for WeatherAPI is not configured.");
        });
        services.AddHttpClient<ITurnstileService, TurnstileService>();

        // Application Services
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

        // Infrastructure Services
        services.AddScoped<IFileStorageService, MinioStorageService>();
        services.AddScoped<IEmailService, BrevoEmailService>();
        services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
        services.AddMemoryCache();

        // Background Jobs
        services.AddHostedService<DeviceInactivityService>();
        services.AddHostedService<WaterStressAnalysisService>();
        services.AddHostedService<DailyTasksService>();
        services.AddHostedService<AdminInactivityService>();

        return services;
    }

    /// <summary>
    ///     Adds custom authentication and authorization services.
    /// </summary>
    public static IServiceCollection AddCustomAuthentication(this IServiceCollection services)
    {
        // 1. AÑADIR IDENTITY PRIMERO
        // Esto configura el sistema principal de usuarios, roles y contraseñas.
        services.AddIdentity<User, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.User.RequireUniqueEmail = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(3);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        
        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.Zero;
        });

        // 2. CONFIGURAR LA AUTENTICACIÓN Y LA COOKIE DE IDENTITY
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";

            // 1. Duración fija de la sesión de 4 horas
            options.ExpireTimeSpan = TimeSpan.FromHours(4);

            // 2. Desactiva la expiración deslizante para que la sesión termine 4 horas después del login, sin importar la actividad.
            options.SlidingExpiration = false;

            // 3. (MUY IMPORTANTE) Activa la validación del SecurityStamp.
            // Esto fuerza a la aplicación a verificar en cada petición si la sesión sigue siendo válida (p. ej. si la contraseña cambió).
            options.Events.OnValidatePrincipal = Microsoft.AspNetCore.Identity.SecurityStampValidator.ValidatePrincipalAsync;
        });

        // Añadimos nuestro esquema personalizado para dispositivos, que es independiente.
        services.AddAuthentication()
            .AddScheme<DeviceAuthenticationOptions, DeviceAuthenticationHandler>(
                DeviceAuthenticationOptions.DefaultScheme, options => { });

        // 3. CONFIGURAR AUTORIZACIÓN (Se mantiene igual)
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
    ///     Adds presentation layer services like Controllers and Views.
    /// </summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddControllersWithViews()
            .AddRazorOptions(options =>
            {
                // Define search locations for area views
                options.AreaViewLocationFormats.Clear();
                options.AreaViewLocationFormats.Add("/Views/{2}/{1}/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Views/{2}/Shared/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
            });

        return services;
    }
}