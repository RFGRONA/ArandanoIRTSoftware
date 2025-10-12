using ArandanoIRT.Web._0_Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

/// <summary>
///     Un controlador base abstracto para los controladores del área de administración.
///     Proporciona funcionalidades comunes, como el manejo estandarizado de los resultados de los servicios
///     y la visualización de notificaciones a través de TempData.
/// </summary>
public abstract class BaseAdminController : Controller
{
    /// <summary>
    ///     Clave para almacenar mensajes de éxito en TempData.
    /// </summary>
    protected const string SuccessMessageKey = "SuccessMessage";

    /// <summary>
    ///     Clave para almacenar mensajes de error en TempData.
    /// </summary>
    protected const string ErrorMessageKey = "ErrorMessage";

    /// <summary>
    ///     Mensaje genérico para solicitudes con datos inválidos.
    /// </summary>
    protected const string InvalidRequestDataMessage = "Los datos de la solicitud son inválidos.";

    /// <summary>
    ///     Maneja el resultado de una operación de servicio.
    ///     Si la operación es exitosa, establece un mensaje de éxito y redirige a la acción especificada.
    ///     Si falla, establece un mensaje de error y devuelve la vista actual con el modelo para no perder los datos
    ///     ingresados por el usuario.
    /// </summary>
    /// <param name="result">El objeto Result devuelto por el servicio.</param>
    /// <param name="successRedirectActionName">El nombre de la acción a la que se redirigirá en caso de éxito.</param>
    /// <param name="modelForFailure">El modelo que se devolverá a la vista en caso de fallo.</param>
    /// <returns>Un `RedirectToAction` en caso de éxito, o una `ViewResult` con el modelo en caso de fallo.</returns>
    protected IActionResult HandleServiceResult(Result result, string successRedirectActionName, object modelForFailure)
    {
        if (result.IsSuccess)
        {
            TempData[SuccessMessageKey] = "Operación completada exitosamente.";
            return RedirectToAction(successRedirectActionName);
        }

        TempData[ErrorMessageKey] = result.ErrorMessage;
        return View(modelForFailure);
    }

    /// <summary>
    ///     Sobrecarga para manejar el resultado de operaciones (como eliminaciones) que no necesitan devolver un modelo en
    ///     caso de fallo.
    ///     Redirige a una acción tanto en caso de éxito como de fallo.
    /// </summary>
    /// <param name="result">El objeto Result devuelto por el servicio.</param>
    /// <param name="successRedirectActionName">El nombre de la acción a la que se redirigirá en caso de éxito.</param>
    /// <param name="failureRedirectActionName">El nombre de la acción a la que se redirigirá en caso de fallo.</param>
    /// <returns>Un `RedirectToAction` en cualquier caso.</returns>
    protected IActionResult HandleServiceResult(Result result, string successRedirectActionName,
        string failureRedirectActionName)
    {
        if (result.IsSuccess)
        {
            TempData[SuccessMessageKey] = "Operación completada exitosamente.";
            return RedirectToAction(successRedirectActionName);
        }

        TempData[ErrorMessageKey] = result.ErrorMessage;
        return RedirectToAction(failureRedirectActionName);
    }
}