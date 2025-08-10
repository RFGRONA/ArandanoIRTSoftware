// En AlertsController.cs

using ArandanoIRT.Web._1_Application.DTOs.Alerts;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Api;

[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly IAlertTriggerService _alertTriggerService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(IConfiguration configuration, ILogger<AlertsController> logger,
        IAlertTriggerService alertTriggerService)
    {
        _configuration = configuration;
        _logger = logger;
        _alertTriggerService = alertTriggerService;
    }

    [HttpPost("grafana-webhook")]
    public async Task<IActionResult> GrafanaWebhook([FromBody] GrafanaWebhookPayload payload)
    {
        // 1. Validar el token de seguridad
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        var secretKey = _configuration["Alerting:GrafanaApiKey"];

        if (string.IsNullOrEmpty(apiKey) || apiKey != secretKey)
        {
            _logger.LogWarning("Intento no autorizado al webhook de Grafana.");
            return Unauthorized();
        }

        _logger.LogInformation("Webhook de Grafana recibido: {AlertName}", payload.AlertName);

        await _alertTriggerService.ProcessGrafanaWebhookAsync(payload);

        return Ok();
    }
}