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

        // Si el usuario está autenticado, el application_name será "user_id_X".
        // Si no, será un valor genérico para peticiones no autenticadas.
        var appName = string.IsNullOrEmpty(userIdClaim)
            ? "arandano_app_unauthenticated"
            : $"user_id_{userIdClaim}";

        try
        {
            // Ejecuta un comando SQL nativo para establecer el application_name en la sesión actual de PostgreSQL.
            await dbContext.Database.ExecuteSqlAsync($"SET application_name = '{appName}'");
        }
        catch (Exception ex)
        {
            // Si la base de datos no está disponible o hay otro error, se registra una advertencia pero no se detiene la petición.
            var logger = context.RequestServices.GetRequiredService<ILogger<UserAuditingMiddleware>>();
            logger.LogWarning(ex, "No se pudo establecer el application_name para la auditoría de la base de datos.");
        }

        // Pasa la petición al siguiente middleware en el pipeline.
        await _next(context);
    }
}