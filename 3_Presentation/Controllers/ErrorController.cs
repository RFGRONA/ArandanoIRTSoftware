using System.Diagnostics;
using ArandanoIRT.Web._3_Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers;

/// <summary>
///     Controlador responsable de manejar y mostrar las páginas de error de la aplicación.
///     Captura tanto excepciones no controladas como errores de código de estado HTTP (ej. 404).
///     Es accesible de forma anónima y se ignora en la documentación de la API.
/// </summary>
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="ErrorController" />.
    /// </summary>
    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///     Muestra la página de error genérica.
    ///     Determina si el error fue causado por una excepción no controlada o por un código de estado HTTP,
    ///     y registra los detalles correspondientes para diagnóstico.
    /// </summary>
    /// <param name="statusCode">El código de estado HTTP que originó el error (opcional).</param>
    /// <returns>La vista de error con un modelo que contiene el ID de la solicitud.</returns>
    [Route("/Error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index(int? statusCode = null)
    {
        var exceptionDetails = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        if (exceptionDetails != null)
            _logger.LogError(exceptionDetails.Error, "Error no controlado en la ruta {Path}. RequestId: {RequestId}",
                exceptionDetails.Path, requestId);
        else if (statusCode.HasValue)
            _logger.LogWarning("Se generó un código de estado de error {StatusCode}. RequestId: {RequestId}",
                statusCode.Value, requestId);

        var viewModel = new ErrorViewModel
        {
            RequestId = requestId
        };

        return View(viewModel);
    }
}