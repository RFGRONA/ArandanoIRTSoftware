using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ArandanoIRT.Web._2_Infrastructure.Services;

/// <summary>
/// Implementación de un servicio que renderiza vistas Razor (.cshtml) a una cadena de texto HTML.
/// Esta clase es fundamental para generar el cuerpo de los correos electrónicos a partir de plantillas,
/// ya que permite hacerlo fuera del contexto de un controlador MVC (por ejemplo, desde un servicio en segundo plano).
/// </summary>
public class RazorViewToStringRenderer : IRazorViewToStringRenderer
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IRazorViewEngine _viewEngine;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="RazorViewToStringRenderer"/>.
    /// </summary>
    public RazorViewToStringRenderer(
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public async Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model)
    {
        var actionContext = GetActionContext();
        var view = FindView(actionContext, viewName);

        await using var output = new StringWriter();
        var viewContext = new ViewContext(
            actionContext,
            view,
            new ViewDataDictionary<TModel>(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary())
            {
                Model = model
            },
            new TempDataDictionary(
                actionContext.HttpContext,
                _tempDataProvider),
            output,
            new HtmlHelperOptions());

        await view.RenderAsync(viewContext);

        return output.ToString();
    }

    /// <summary>
    /// Busca una vista Razor por su nombre o ruta.
    /// </summary>
    /// <param name="actionContext">El contexto de la acción actual.</param>
    /// <param name="viewName">El nombre o ruta de la vista a encontrar.</param>
    /// <returns>La instancia de IView encontrada.</returns>
    /// <exception cref="InvalidOperationException">Se lanza si la vista no se encuentra en ninguna de las ubicaciones buscadas.</exception>
    private IView FindView(ActionContext actionContext, string viewName)
    {
        var getViewResult = _viewEngine.GetView(null, viewName, true);
        if (getViewResult.Success) return getViewResult.View;

        var findViewResult = _viewEngine.FindView(actionContext, viewName, true);
        if (findViewResult.Success) return findViewResult.View;

        var searchedLocations = getViewResult.SearchedLocations.Concat(findViewResult.SearchedLocations);
        var errorMessage = string.Join(
            Environment.NewLine,
            new[] { $"No se pudo encontrar la vista '{viewName}'. Se buscó en las siguientes ubicaciones:" }.Concat(
                searchedLocations));

        throw new InvalidOperationException(errorMessage);
    }

    /// <summary>
    /// Obtiene un ActionContext válido para el motor de vistas.
    /// Si existe un HttpContext actual (porque se ejecuta en una petición web), lo utiliza.
    /// Si no, crea un HttpContext falso para permitir la renderización desde un proceso en segundo plano.
    /// </summary>
    /// <returns>Una instancia de ActionContext.</returns>
    private ActionContext GetActionContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            var routeData = new RouteData();
            var actionDescriptor = new ActionDescriptor();
            var actionContext = new ActionContext(new DefaultHttpContext { RequestServices = _serviceProvider },
                routeData, actionDescriptor);
            return actionContext;
        }

        return new ActionContext(httpContext, httpContext.GetRouteData()!, new ActionDescriptor());
    }
}