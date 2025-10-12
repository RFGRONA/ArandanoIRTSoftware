namespace ArandanoIRT.Web._3_Presentation.ViewModels;

/// <summary>
///     ViewModel para la página de error genérica de la aplicación.
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    ///     El identificador único de la solicitud que causó el error.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    ///     Propiedad computada que indica si se debe mostrar el RequestId en la vista.
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>
    ///     Un mensaje de error descriptivo para mostrar al usuario.
    /// </summary>
    public string? Message { get; set; }
}