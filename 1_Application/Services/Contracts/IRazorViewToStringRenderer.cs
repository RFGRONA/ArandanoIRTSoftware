namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
/// Define el contrato para un servicio de utilidad que renderiza vistas de Razor (.cshtml) a una cadena de texto (string).
/// Esencialmente, convierte una vista en HTML puro, lo cual es muy útil para generar el cuerpo de correos electrónicos.
/// </summary>
public interface IRazorViewToStringRenderer
{
    /// <summary>
    /// Renderiza una vista de Razor especificada a una cadena de texto HTML.
    /// </summary>
    /// <typeparam name="TModel">El tipo del modelo de datos que se pasará a la vista.</typeparam>
    /// <param name="viewName">La ruta de la vista Razor a renderizar (ej. "/Views/Emails/Welcome.cshtml").</param>
    /// <param name="model">El objeto del modelo con los datos que la vista necesita para renderizarse.</param>
    /// <returns>Una cadena de texto que contiene el HTML resultante de la renderización de la vista.</returns>
    Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model);
}