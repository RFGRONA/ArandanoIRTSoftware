using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArandanoIRT.Web._3_Presentation.Attributes;

public class ValidateTurnstileAttribute : ActionFilterAttribute
{
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

        // Log para saber si el atributo se activó y qué token recibió
        logger.LogInformation("Atributo [ValidateTurnstile] activado. Token recibido: '{Token}'", token);

        var turnstileService = context.HttpContext.RequestServices.GetRequiredService<ITurnstileService>();

        if (string.IsNullOrEmpty(token) || !await turnstileService.IsTokenValid(token))
        {
            context.ModelState.AddModelError("Turnstile", "La verificación de seguridad ha fallado. Por favor, inténtelo de nuevo.");
            // Si el controlador devuelve una vista, lo reenviamos a la misma vista con el error.
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