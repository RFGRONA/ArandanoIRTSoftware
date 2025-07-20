using ArandanoIRT.Web._0_Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

/// <summary>
/// A base controller for admin area controllers to share common functionality,
/// such as handling service results and displaying notifications.
/// </summary>
public abstract class BaseAdminController : Controller
{
    protected const string SuccessMessageKey = "SuccessMessage";
    protected const string ErrorMessageKey = "ErrorMessage";
    protected const string InvalidRequestDataMessage = "Invalid request data.";

    /// <summary>
    /// Handles the result of a service operation, setting TempData messages
    /// and returning the appropriate IActionResult.
    /// </summary>
    /// <param name="result">The result object from the service layer.</param>
    /// <param name="successRedirectActionName">The name of the action to redirect to on success.</param>
    /// <param name="modelForFailure">The view model to return to the view on failure.</param>
    /// <returns>A RedirectToAction on success, or a View with the model on failure.</returns>
    protected IActionResult HandleServiceResult(Result result, string successRedirectActionName, object modelForFailure)
    {
        if (result.IsSuccess)
        {
            // You can customize the success message here if needed
            TempData[SuccessMessageKey] = "Operation completed successfully.";
            return RedirectToAction(successRedirectActionName);
        }
        else
        {
            TempData[ErrorMessageKey] = result.ErrorMessage;
            // By returning the model, the user doesn't lose the data they entered.
            return View(modelForFailure);
        }
    }

    /// <summary>
    /// Overload for delete operations or actions that don't return a model on failure.
    /// </summary>
    protected IActionResult HandleServiceResult(Result result, string successRedirectActionName, string failureRedirectActionName)
    {
        if (result.IsSuccess)
        {
            TempData[SuccessMessageKey] = "Operation completed successfully.";
            return RedirectToAction(successRedirectActionName);
        }
        else
        {
            TempData[ErrorMessageKey] = result.ErrorMessage;
            return RedirectToAction(failureRedirectActionName);
        }
    }
}
