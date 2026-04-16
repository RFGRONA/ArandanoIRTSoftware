namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IRazorViewToStringRenderer
{
    Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model);
}