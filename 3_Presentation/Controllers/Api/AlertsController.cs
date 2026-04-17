// En AlertsController.cs

using ArandanoIRT.Web._1_Application.DTOs.Alerts;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Api;

/// <summary>
///     Controlador de API para recibir notificaciones de alerta de sistemas externos.
/// </summary>
[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly IAlertTriggerService _alertTriggerService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AlertsController> _logger;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="AlertsController" />.
    /// </summary>
    public AlertsController(IConfiguration configuration, ILogger<AlertsController> logger,
        IAlertTriggerService alertTriggerService)
    {
        _configuration = configuration;
        _logger = logger;
        _alertTriggerService = alertTriggerService;
    }

    /// <summary>
    ///     Endpoint para recibir webhooks de alerta enviados por Grafana.
    ///     Valida una clave de API secreta en el encabezado `X-Api-Key` para asegurar que la petición es legítima.
    ///     Si la validación es exitosa, delega el procesamiento de la alerta al servicio correspondiente.
    /// </summary>
    /// <param name="payload">El cuerpo de la petición (payload) enviado por Grafana, deserializado a un objeto.</param>
    /// <returns>Un resultado `Unauthorized (401)` si la clave de API es incorrecta, o `Ok (200)` si la alerta se procesa.</returns>
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