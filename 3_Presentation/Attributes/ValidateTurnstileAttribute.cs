using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArandanoIRT.Web._3_Presentation.Attributes;

/// <summary>
///     Un atributo de filtro de acción que intercepta las peticiones para validar el token de Cloudflare Turnstile.
///     Se utiliza para proteger los formularios contra envíos automatizados por bots.
///     La validación se omite automáticamente en el entorno de desarrollo.
/// </summary>
public class ValidateTurnstileAttribute : ActionFilterAttribute
{
    /// <summary>
    ///     Se ejecuta antes de que se ejecute la acción del controlador.
    ///     Extrae el token de Turnstile del formulario, lo valida usando ITurnstileService y, si la validación falla,
    ///     detiene la ejecución y devuelve la vista con un error en el ModelState.
    /// </summary>
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var environment = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

        if (environment.IsDevelopment())
        {
            await base.OnActionExecutionAsync(context, next);
            return;
        }

        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ValidateTurnstileAttribute>>();
        var token = context.HttpContext.Request.Form["cf-turnstile-response"].ToString();

        logger.LogInformation("Atributo [ValidateTurnstile] activado. Token recibido: '{Token}'", token);

        var turnstileService = context.HttpContext.RequestServices.GetRequiredService<ITurnstileService>();

        if (string.IsNullOrEmpty(token) || !await turnstileService.IsTokenValid(token))
        {
            context.ModelState.AddModelError("Turnstile",
                "La verificación de seguridad ha fallado. Por favor, inténtelo de nuevo.");

            if (context.Controller is Controller controller)
            {
                var originalModel = context.ActionArguments.Any() ? context.ActionArguments.Values.First() : null;
                context.Result = controller.View(originalModel);
            }
            else
            {
                context.Result = new BadRequestObjectResult("La verificación de Turnstile ha fallado.");
            }

            return;
        }

        await base.OnActionExecutionAsync(context, next);
    }
}