using System.Security.Claims;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web.Common;
using Microsoft.AspNetCore.Identity;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class SupportService : ISupportService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SupportService> _logger;
    private readonly IRazorViewToStringRenderer _renderer;
    private readonly UserManager<User> _userManager;
    private readonly IUserService _userService;

    public SupportService(IUserService userService, IEmailService emailService, UserManager<User> userManager,
        ILogger<SupportService> logger, IRazorViewToStringRenderer renderer)
    {
        _userService = userService;
        _emailService = emailService;
        _userManager = userManager;
        _logger = logger;
        _renderer = renderer;
    }

    public async Task<Result> ProcessPublicHelpRequestAsync(PublicHelpRequestDto model)
    {
        var adminsToNotify = await _userService.GetAdminsToNotifyForHelpRequestsAsync();
        if (!adminsToNotify.Any())
        {
            _logger.LogWarning(
                "Se recibió una solicitud de ayuda pública, pero no hay administradores configurados para recibir la notificación.");
            return Result.Success();
        }

        // Renderizar la plantilla de correo
        var htmlContent = await _renderer.RenderViewToStringAsync(
            "/Views/Shared/EmailTemplates/_PublicHelpRequestEmail.cshtml",
            model
        );

        foreach (var admin in adminsToNotify)
        {
            var subject = $"[Ayuda Pública] {model.Subject}";
            await _emailService.SendEmailAsync(admin.Email, $"{admin.FirstName} {admin.LastName}", subject,
                htmlContent);
        }

        return Result.Success();
    }


    public async Task<Result> ProcessAuthenticatedHelpRequestAsync(AuthenticatedHelpRequestDto model,
        ClaimsPrincipal userPrincipal)
    {
        var currentUser = await _userManager.GetUserAsync(userPrincipal);
        if (currentUser == null) return Result.Failure("Usuario no autenticado.");

        var adminsToNotify = await _userService.GetAdminsToNotifyForHelpRequestsAsync();
        if (!adminsToNotify.Any())
        {
            _logger.LogWarning("El usuario {UserId} solicitó ayuda, pero no hay administradores para notificar.",
                currentUser.Id);
            return Result.Success();
        }

        // Crear el modelo para la vista
        var emailModel = (Request: model, RequestingUser: currentUser);
        // Renderizar la plantilla de correo
        var htmlContent = await _renderer.RenderViewToStringAsync(
            "/Views/Shared/EmailTemplates/_AuthenticatedHelpRequestEmail.cshtml",
            emailModel
        );

        foreach (var admin in adminsToNotify)
        {
            var subject = $"[Ayuda Usuario] {model.Subject}";
            await _emailService.SendEmailAsync(admin.Email, $"{admin.FirstName} {admin.LastName}", subject,
                htmlContent);
        }

        return Result.Success();
    }
}