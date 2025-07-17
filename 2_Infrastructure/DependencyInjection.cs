using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._1_Application.Services.Implementation;
using ArandanoIRT.Web._2_Infrastructure.Authentication;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._2_Infrastructure.Services;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.Cookies;
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

        // HTTP Client for Weather API
        services.AddHttpClient("WeatherApi", (serviceProvider, client) =>
        {
            var weatherApiSettings = configuration.GetSection(WeatherApiSettings.SectionName).Get<WeatherApiSettings>();
            if (weatherApiSettings != null && !string.IsNullOrEmpty(weatherApiSettings.BaseUrl))
                client.BaseAddress = new Uri(weatherApiSettings.BaseUrl);
            else
                Log.Warning("BaseUrl for WeatherAPI is not configured.");
        });

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

        // Infrastructure Services
        services.AddScoped<IFileStorageService, MinioStorageService>();
        services.AddScoped<IEmailService, BrevoEmailService>();
        services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();

        return services;
    }

    /// <summary>
    ///     Adds custom authentication and authorization services.
    /// </summary>
    public static IServiceCollection AddCustomAuthentication(this IServiceCollection services)
    {
        // 1. CONFIGURAR LA AUTENTICACIÓN Y LA COOKIE PRIMERO
        // Esto registra el manejador para el esquema "Cookies" que el BootstrapController necesita.
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Admin/Account/Login";
                options.LogoutPath = "/Admin/Account/Logout";
                options.AccessDeniedPath = "/Admin/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
                options.SlidingExpiration = true;
            })
            // Mantenemos el esquema para dispositivos
            .AddScheme<DeviceAuthenticationOptions, DeviceAuthenticationHandler>(
                DeviceAuthenticationOptions.DefaultScheme,
                options => { }
            );

        // 2. AÑADIR IDENTITY
        // Identity se construirá sobre la configuración de autenticación que ya hemos definido.
        services.AddIdentity<User, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // 3. CONFIGURAR AUTORIZACIÓN (Se mantiene igual)
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("BootstrapOnly", policy => policy.RequireRole("BootstrapAdmin"));
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