namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface ITurnstileService
{
    Task<bool> IsTokenValid(string token);
}