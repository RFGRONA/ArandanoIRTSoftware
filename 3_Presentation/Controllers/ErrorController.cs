using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ArandanoIRT.Web._3_Presentation.ViewModels; 
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authorization; 

namespace ArandanoIRT.Web._3_Presentation.Controllers;

[AllowAnonymous] 
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [Route("/Error")] 
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index(int? statusCode = null)
    {
        var exceptionDetails = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        if (exceptionDetails != null)
        {
            _logger.LogError(exceptionDetails.Error, "Error no controlado en la ruta {Path}. RequestId: {RequestId}", exceptionDetails.Path, requestId);
        }
        else if (statusCode.HasValue)
        {
            _logger.LogWarning("Se generó un código de estado de error {StatusCode}. RequestId: {RequestId}", statusCode.Value, requestId);
        }

        var viewModel = new ErrorViewModel
        {
            RequestId = requestId
        };

        return View(viewModel);
    }
}