using ArandanoIRT.Web._2_Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._1_Application.Services.Implementation;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._2_Infrastructure.Middleware;
using ArandanoIRT.Web._2_Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog.Formatting.Json;

// 0. Configuración de Serilog (antes de crear el builder)
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext() // Enriquece los logs con datos del contexto (ej. en un request)
    .Enrich.WithMachineName() // Añade el nombre del host (útil en contenedores)
    .WriteTo.Console(new JsonFormatter()) // Escribe en la consola usando formato JSON
    .CreateBootstrapLogger(); // Lo crea como un logger de "arranque"

try
{
    Log.Information("Iniciando la aplicación...");

    var builder = WebApplication.CreateBuilder(args);

    // Cargar configuración de Serilog desde appsettings.json y variables de entorno
    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            // Lee la configuración adicional desde appsettings.json si es necesario.
            .ReadFrom.Configuration(context.Configuration)

            // Establece el nivel mínimo de log. Es bueno ponerlo en Debug para desarrollo.
            .MinimumLevel.Debug()

            // Suprime el ruido de los logs internos de .NET y EF Core. 
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)

            // Enriquece los logs con información útil.
            .Enrich.FromLogContext()
            .Enrich.WithMachineName() 
            
            .WriteTo.Console(
                formatter: new JsonFormatter(), // Formato JSON para Promtail/Loki.
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel
                    .Information // Loguea de Information para arriba en consola.
            );
    });
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

    // 1. Configuración de Servicios
    // Mapear secciones de appsettings.json a clases de opciones
    builder.Services.Configure<AdminCredentialsSettings>(
        builder.Configuration.GetSection(AdminCredentialsSettings.SectionName));
    builder.Services.Configure<WeatherApiSettings>(
        builder.Configuration.GetSection(WeatherApiSettings.SectionName));
    builder.Services.Configure<TokenSettings>(
        builder.Configuration.GetSection(TokenSettings.SectionName));
    builder.Services.Configure<MinioSettings>(
        builder.Configuration.GetSection(MinioSettings.SectionName));
    
    // Configuración de Autenticación por Cookies para el Admin Web
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Admin/Login"; // Página de login para el admin
            options.LogoutPath = "/Admin/Logout";
            options.AccessDeniedPath = "/Admin/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromDays(1); // Duración de la cookie
            options.SlidingExpiration = true;
        })
        .AddScheme<DeviceAuthenticationOptions, DeviceAuthenticationHandler>(
            DeviceAuthenticationOptions.DefaultScheme, // "DeviceAuthScheme"
            options => { }
        );

    // Configuración de autorización (si quieres usar roles o políticas más adelante)
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("DeviceAuthenticated", policy =>
        {
            policy.AddAuthenticationSchemes(DeviceAuthenticationOptions.DefaultScheme);
            policy.RequireAuthenticatedUser(); // Requiere que el esquema "DeviceAuthScheme" haya autenticado al usuario.
            policy.RequireRole("Device"); // Opcional: requerir el rol "Device" que asignamos en el handler
        });
    });
    
    builder.Services.AddHttpClient("WeatherApi", client =>
    {
        // La BaseUrl se obtiene de la configuración inyectada en el servicio,
        // o puedes configurarla aquí si es fija.
        // Si WeatherApiSettings.BaseUrl ya tiene "https://api.weatherapi.com/v1", entonces aquí no necesitas ponerla.
        // O puedes construirla aquí:
        var weatherApiSettings = builder.Configuration.GetSection(WeatherApiSettings.SectionName).Get<WeatherApiSettings>();
        if (weatherApiSettings != null && !string.IsNullOrEmpty(weatherApiSettings.BaseUrl))
        {
            client.BaseAddress = new Uri(weatherApiSettings.BaseUrl);
        }
        // else { Log.Warning("BaseUrl para WeatherAPI no configurada."); } // No puedes usar ILogger aquí
    });

    // Add services to the container.
    builder.Services.AddControllersWithViews();
    builder.Services.AddHttpContextAccessor(); // Útil para acceder a HttpContext desde servicios


    // TODO: Registrar tus propios servicios (ej: DeviceService, ThermalDataService, etc.)
    builder.Services.AddScoped<IWeatherService, WeatherService>();
    builder.Services.AddScoped<IDeviceService, DeviceService>();
    builder.Services.AddScoped<IDataSubmissionService, DataSubmissionService>();
    builder.Services.AddScoped<ICropService, CropService>();
    builder.Services.AddScoped<IPlantService, PlantService>();
    builder.Services.AddScoped<IDeviceAdminService, DeviceAdminService>();
    builder.Services.AddScoped<IDataQueryService, DataQueryService>();
    builder.Services.AddScoped<IFileStorageService, MinioStorageService>();
    
    builder.Services.AddControllersWithViews()
        .AddRazorOptions(options =>
        {
            options.AreaViewLocationFormats.Clear();
            options.AreaViewLocationFormats.Add("/Views/{2}/{1}/{0}.cshtml");
            options.AreaViewLocationFormats.Add("/Views/{2}/Shared/{0}.cshtml");
            options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
        });

    var app = builder.Build();

    // 2. Configuración del Pipeline HTTP
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage(); // Muestra errores detallados en desarrollo
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseSerilogRequestLogging(); 

    app.UseAuthentication(); // Importante: ANTES de UseAuthorization
    app.UseAuthorization();
    
    app.UseMiddleware<UserAuditingMiddleware>();

    // Ruta para el área de Admin (ejemplo)
    app.MapControllerRoute(
        name: "admin_area",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        // Cambiamos el controlador y acción por defecto para que apunten al login del Admin.
        // Si el usuario ya está autenticado, el propio AccountController/Login lo redirigirá.
        pattern: "{controller=Account}/{action=Login}/{id?}",
        defaults: new { area = "Admin" }); // Asegúrate de que esta ruta por defecto también caiga en el área Admin

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al iniciar.");
}
finally
{
    Log.CloseAndFlush();
}