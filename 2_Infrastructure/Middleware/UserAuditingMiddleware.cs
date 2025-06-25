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
        // Intentamos obtener el ID del usuario autenticado desde los Claims de la petición.
        // El tipo de claim puede variar según cómo configures la identidad.
        // "NameIdentifier" es común para el ID.
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        string appName = "arandano_app_unauthenticated"; // Valor por defecto

        if (!string.IsNullOrEmpty(userIdClaim))
        {
            // Si encontramos un usuario, construimos un identificador único.
            appName = $"user_id_{userIdClaim}";
        }

        // Usamos el DbContext para obtener la conexión a la base de datos
        // y ejecutar un comando SQL crudo para establecer el parámetro de la sesión.
        // Este ajuste solo dura para la vida de esta conexión (es decir, esta petición).
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"SET application_name = '{appName}';";
            await cmd.ExecuteNonQueryAsync();
        }

        // Pasamos la petición al siguiente middleware en el pipeline.
        await _next(context);
    }
}