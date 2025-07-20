using ArandanoIRT.Web._0_Domain.Common;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IEmailService
{
    Task<Result> SendEmailAsync(string toEmail, string toName, string subject, string htmlContent);
}