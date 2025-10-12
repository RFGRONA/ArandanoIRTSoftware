using ArandanoIRT.Web._2_Infrastructure;
using ArandanoIRT.Web._2_Infrastructure.Middleware;
using Hangfire;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

// Configuración inicial de Serilog para el arranque de la aplicación.
// Permite capturar logs incluso antes de que la configuración principal sea leída.
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console(new JsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando la aplicación...");

    var builder = WebApplication.CreateBuilder(args);

    // Configuración completa de Serilog utilizando el archivo appsettings.json.
    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .WriteTo.Console(
                new JsonFormatter(),
                LogEventLevel.Information
            );
    });

    // Se registran todos los servicios de la aplicación utilizando los métodos de extensión.
    builder.Services
        .AddInfrastructure(builder.Configuration)
        .AddCustomAuthentication()
        .AddPresentation();

    var app = builder.Build();

    // Se configura el pipeline de peticiones HTTP (middleware).
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseHangfireDashboard();
    app.UseMiddleware<UserAuditingMiddleware>();

    // Se configuran las rutas de los controladores.
    app.MapControllerRoute(
        "admin_default",
        "{controller=Dashboard}/{action=Index}/{id?}",
        new { area = "Admin" });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al iniciar.");
}
finally
{
    // Asegura que todos los logs en buffer se escriban antes de cerrar la aplicación.
    Log.CloseAndFlush();
}