using System.Security.Claims;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Identity;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

/// <summary>
///     Implementación del servicio de soporte técnico.
///     Procesa las solicitudes de ayuda y coordina el envío de notificaciones a los administradores correspondientes.
/// </summary>
public class SupportService : ISupportService
{
    private readonly IAlertService _alertService;
    private readonly UserManager<User> _userManager;
    private readonly IUserService _userService;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="SupportService" />.
    /// </summary>
    /// <param name="alertService">Servicio para enviar las notificaciones por correo.</param>
    /// <param name="userManager">Servicio para la gestión de usuarios de ASP.NET Core Identity.</param>
    /// <param name="userService">Servicio para consultar información de usuarios de la aplicación.</param>
    public SupportService(IAlertService alertService, UserManager<User> userManager, IUserService userService)
    {
        _alertService = alertService;
        _userManager = userManager;
        _userService = userService;
    }

    /// <inheritdoc />
    public async Task<Result> ProcessPublicHelpRequestAsync(PublicHelpRequestDto model)
    {
        var adminsToNotify = await _userService.GetAdminsToNotifyAsync(s => s.EmailOnHelpRequest);
        await _alertService.SendPublicHelpRequestEmailAsync(model, adminsToNotify);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ProcessAuthenticatedHelpRequestAsync(AuthenticatedHelpRequestDto model,
        ClaimsPrincipal userPrincipal)
    {
        var currentUser = await _userManager.GetUserAsync(userPrincipal);
        if (currentUser == null) return Result.Failure("Usuario no autenticado.");

        var adminsToNotify = await _userService.GetAdminsToNotifyAsync(s => s.EmailOnHelpRequest);
        await _alertService.SendAuthenticatedHelpRequestEmailAsync(model, currentUser, adminsToNotify);

        return Result.Success();
    }
}