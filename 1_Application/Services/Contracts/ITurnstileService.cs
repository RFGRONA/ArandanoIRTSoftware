namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
/// Define el contrato para un servicio que valida los tokens de Cloudflare Turnstile.
/// Turnstile es una alternativa a CAPTCHA para proteger los formularios de envíos automáticos (bots).
/// </summary>
public interface ITurnstileService
{
    /// <summary>
    /// Verifica si un token de Turnstile proporcionado por el cliente es válido,
    /// enviándolo al endpoint de verificación de Cloudflare.
    /// </summary>
    /// <param name="token">El token generado por el widget de Turnstile en el lado del cliente.</param>
    /// <returns>Verdadero (true) si el token es válido, de lo contrario, falso (false).</returns>
    Task<bool> IsTokenValid(string token);
}