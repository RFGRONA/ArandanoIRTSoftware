using ArandanoIRT.Web._2_Infrastructure;
using ArandanoIRT.Web._2_Infrastructure.Middleware;
using Hangfire;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

// Configure Serilog for bootstrap logging
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console(new JsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting application...");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings.json
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

    // 1. Configure Services using Extension Methods
    builder.Services
        .AddInfrastructure(builder.Configuration)
        .AddCustomAuthentication()
        .AddPresentation();

    var app = builder.Build();

    // 2. Configure HTTP request pipeline
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

    // Configure endpoints
    app.MapControllerRoute(
        "admin_default",
        "{controller=Dashboard}/{action=Index}/{id?}",
        new { area = "Admin" });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start.");
}
finally
{
    Log.CloseAndFlush();
}