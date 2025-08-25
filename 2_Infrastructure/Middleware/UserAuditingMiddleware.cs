using System.Security.Claims;
using ArandanoIRT.Web._2_Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._2_Infrastructure.Middleware;

public class UserAuditingMiddleware
{
    private readonly RequestDelegate _next;

    public UserAuditingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        var appName = "arandano_app_unauthenticated";
        if (!string.IsNullOrEmpty(userIdClaim)) appName = $"user_id_{userIdClaim}";

        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("SET application_name = {0}", appName);
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<UserAuditingMiddleware>>();
            logger.LogWarning(ex, "No se pudo establecer el application_name para la auditoría.");
        }

        await _next(context);
    }
}