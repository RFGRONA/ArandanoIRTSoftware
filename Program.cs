using ArandanoIRT.Web.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using ArandanoIRT.Web.Configuration;
using ArandanoIRT.Web.Services.Contracts;
using ArandanoIRT.Web.Services.Implementation;
using Microsoft.Extensions.Options; // Para tus clases de settings

// 0. Configuración de Serilog (antes de crear el builder)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/ThermalAppLog-.txt", // Crea una carpeta 'logs' en la raíz del proyecto
        rollingInterval: RollingInterval.Day, // Nuevo archivo cada día
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateBootstrapLogger(); // Para loguear problemas durante el inicio

try
{
    Log.Information("Iniciando la aplicación ThermalDataApp...");

    var builder = WebApplication.CreateBuilder(args);

    // Cargar configuración de Serilog desde appsettings.json y variables de entorno
    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        // .ReadFrom.Configuration(context.Configuration) // Podrías omitir esto si todo es programático
        .MinimumLevel.Debug() // Nivel mínimo global
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning) // Silenciar logs de Microsoft
        .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        // Nivel específico para tu aplicación si es necesario (aunque .Debug() global ya lo cubriría)
        // .MinimumLevel.Override("AIRTProvisional", Serilog.Events.LogEventLevel.Debug) 
        .Enrich.FromLogContext()
        .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug) // Nivel para la consola
        .WriteTo.File(
            path: builder.Configuration.GetValue<string>("SerilogFileSink:Path") ?? "logs/ThermalAppLog-.txt", // Considera una sección de config más simple
            rollingInterval: RollingInterval.Day,
            outputTemplate: builder.Configuration.GetValue<string>("SerilogFileSink:OutputTemplate") ??
                            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug // Nivel para el archivo
        )
    );


    // 1. Configuración de Servicios
    // Mapear secciones de appsettings.json a clases de opciones
    builder.Services.Configure<SupabaseSettings>(
        builder.Configuration.GetSection(SupabaseSettings.SectionName));
    builder.Services.Configure<AdminCredentialsSettings>(
        builder.Configuration.GetSection(AdminCredentialsSettings.SectionName));
    builder.Services.Configure<WeatherApiSettings>(
        builder.Configuration.GetSection(WeatherApiSettings.SectionName));
    builder.Services.Configure<TokenSettings>(
        builder.Configuration.GetSection(TokenSettings.SectionName));

    // Registrar el cliente de Supabase para Inyección de Dependencias
    builder.Services.AddSingleton(provider =>
    {
        // Recupera SupabaseSettings a través de IOptions<T> del proveedor de servicios
        var supabaseSettings = provider.GetRequiredService<IOptions<SupabaseSettings>>().Value;

        var options = new Supabase.SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false, // Ajusta según necesites Realtime
            // Otras opciones...
        };

        // Añadir el header de autorización con la ServiceRoleKey a las opciones
        options.Headers ??= new Dictionary<string, string>(); // Asegura que el diccionario no sea null

        if (!string.IsNullOrEmpty(supabaseSettings.ServiceRoleKey))
        {
            options.Headers["Authorization"] = $"Bearer {supabaseSettings.ServiceRoleKey}";
            // Nota: No puedes usar _logger aquí porque no está inyectado en este contexto DI
            // Puedes loguear usando Serilog estático si es crucial, pero la configuración silenciosa es común.
            // Log.Information("Configurando ServiceRoleKey para Supabase client.");
        }
        else
        {
            Log.Warning("ServiceRoleKey no está configurada. Las operaciones de Postgrest pueden fallar si RLS lo requiere.");
        }

        // Crea el cliente Supabase con la URL, PublicApiKey y las opciones configuradas
        var client = new Supabase.Client(
            supabaseSettings.Url,
            supabaseSettings.PublicApiKey, // Usa la Public (anon) Key aquí
            options); // Pasa las opciones que ahora incluyen el header si aplica

        Log.Information("Cliente de Supabase inicializado con URL: {SupabaseUrl}", supabaseSettings.Url);
        return client;
    });


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
    
    builder.Services.AddControllersWithViews()
        .AddRazorOptions(options =>
        {
            // {2} es el nombre del Controlador
            // {1} es el nombre del Área
            // {0} es el nombre de la Vista
            options.AreaViewLocationFormats.Clear(); // Limpiamos las convenciones por defecto si es necesario
            options.AreaViewLocationFormats.Add("/Views/{2}/{1}/{0}.cshtml");         // Para vistas específicas del controlador de área
            options.AreaViewLocationFormats.Add("/Views/{2}/Shared/{0}.cshtml");     // Para vistas compartidas dentro del área
            options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");         // Para vistas compartidas globales (ya debería estar)
            // Si también usas Layouts específicos para áreas que no están en Shared global:
            // options.AreaPageViewLocationFormats.Add("/Views/{1}/Shared/{0}.cshtml"); // Para layouts de área
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

    app.UseSerilogRequestLogging(); // Loguea cada petición HTTP

    app.UseAuthentication(); // Importante: ANTES de UseAuthorization
    app.UseAuthorization();

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
    Log.Fatal(ex, "La aplicación ThermalDataApp falló al iniciar.");
}
finally
{
    Log.CloseAndFlush();
}