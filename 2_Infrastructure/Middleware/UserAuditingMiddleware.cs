using System.Security.Claims;
using ArandanoIRT.Web._2_Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._2_Infrastructure.Middleware;

/// <summary>
///     Middleware de ASP.NET Core que intercepta cada petición para establecer el `application_name` en la conexión de la
///     base de datos.
///     Esto permite auditar qué usuario está realizando las operaciones a nivel de la base de datos (PostgreSQL).
/// </summary>
public class UserAuditingMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="UserAuditingMiddleware" />.
    /// </summary>
    /// <param name="next">El siguiente middleware en el pipeline de la petición.</param>
    public UserAuditingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    ///     Método principal del middleware que se ejecuta en cada petición HTTP.
    /// </summary>
    /// <param name="context">El HttpContext de la petición actual.</param>
    /// <param name="dbContext">El contexto de la base de datos, inyectado para esta petición.</param>
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