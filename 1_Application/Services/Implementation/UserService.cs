using System.Linq.Expressions;
using System.Security.Claims;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

/// <summary>
///     Implementación del servicio de gestión de usuarios.
///     Centraliza toda la lógica de negocio para el registro, autenticación, gestión de perfiles,
///     y acciones administrativas sobre los usuarios, utilizando ASP.NET Core Identity.
/// </summary>
public class UserService : IUserService
{
    private readonly IAlertService _alertService;
    private readonly ApplicationDbContext _context;
    private readonly IInvitationService _invitationService;
    private readonly ILogger<UserService> _logger;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="UserService" />.
    /// </summary>
    public UserService(
        ApplicationDbContext context,
        UserManager<User> userManager,
        RoleManager<ApplicationRole> roleManager,
        SignInManager<User> signInManager,
        IInvitationService invitationService,
        IAlertService alertService,
        ILogger<UserService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _invitationService = invitationService;
        _logger = logger;
        _alertService = alertService;
    }

    /// <inheritdoc />
    public async Task<(SignInResult Result, bool JustLockedOut)> LoginUserAsync(LoginDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, true);
            return (result, false);
        }

        // 1. Verificamos si al usuario le queda solo un intento antes de ser bloqueado.
        var isAboutToLockOut = user.AccessFailedCount == _userManager.Options.Lockout.MaxFailedAccessAttempts - 1;

        // 2. Realizamos el intento de inicio de sesión.
        var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, true, true);

        // 3. Si el intento falló y resultó en un bloqueo, y sabíamos que estaba a punto de ocurrir,
        //    marcamos el resultado para que el controlador pueda enviar la alerta.
        if (isAboutToLockOut && signInResult.IsLockedOut)
        {
            _logger.LogWarning("La cuenta para {Email} ha sido bloqueada en este intento.", user.Email);
            return (signInResult, true);
        }

        return (signInResult, false);
    }

    /// <inheritdoc />
    public async Task<Result> RegisterUserAsync(RegisterDto model)
    {
        // 1. Validar el código de invitación antes de cualquier otra operación.
        var invitationResult = await _invitationService.ValidateCodeAsync(model.InvitationCode, model.Email);
        if (invitationResult.IsFailure) return Result.Failure(invitationResult.ErrorMessage);
        var invitation = invitationResult.Value;

        // 2. Crear la entidad del usuario.
        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (invitation.IsAdmin)
        {
            user.AccountSettings.EmailOnHelpRequest = true;
            user.AccountSettings.EmailOnAppFailureAlert = true;
            user.AccountSettings.EmailOnDeviceFailureAlert = true;
            user.AccountSettings.EmailOnDeviceInactivity = true;
        }

        var identityResult = await _userManager.CreateAsync(user, model.Password);

        if (!identityResult.Succeeded)
        {
            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        _logger.LogInformation("Usuario {Email} creado en la base de datos.", user.Email);

        // 3. Realizar operaciones secundarias (asignar rol, anular código).
        try
        {
            if (invitation.IsAdmin)
            {
                if (!await _roleManager.RoleExistsAsync("Admin"))
                    await _roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            await _invitationService.MarkCodeAsUsedAsync(invitation.Id);
        }
        catch (Exception ex)
        {
            // Si algo falla después de crear el usuario, se debe revertir la creación para mantener la consistencia.
            _logger.LogError(ex, "Error en operaciones secundarias para {Email}. Revirtiendo creación.", user.Email);
            await _userManager.DeleteAsync(user);
            return Result.Failure("Ocurrió un error al finalizar el registro. Por favor, intente de nuevo.");
        }

        // 4. Si todo el proceso fue exitoso, iniciar sesión con el nuevo usuario.
        await _signInManager.SignInAsync(user, false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SelectListItem>> GetUsersForSelectionAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.FirstName + " " + u.LastName
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result<(string Name, string ResetLink)>> GeneratePasswordResetAsync(ForgotPasswordDto model,
        IUrlHelper urlHelper, string scheme)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        // Nota de Seguridad: Si el usuario no existe, no se devuelve un error explícito.
        // Esto previene que un atacante pueda usar el formulario para descubrir qué correos están registrados.
        if (user == null)
            return Result.Success(("", ""));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var callbackUrl = urlHelper.Action(
            "ResetPassword",
            "Account",
            new { token, email = user.Email },
            scheme);

        if (string.IsNullOrEmpty(callbackUrl))
            return Result.Failure<(string, string)>("No se pudo generar la URL de reseteo.");

        return Result.Success((user.FirstName, callbackUrl));
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(ResetPasswordDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return Result.Failure("Ocurrió un error. Por favor, intente de nuevo.");

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await _alertService.SendPasswordChangedEmailAsync(user.Email, user.FirstName);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ChangePasswordAsync(ClaimsPrincipal userPrincipal, ChangePasswordDto model)
    {
        var user = await _userManager.GetUserAsync(userPrincipal);
        if (user == null) return Result.Failure("Usuario no encontrado.");

        var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await _alertService.SendPasswordChangedEmailAsync(user.Email, user.FirstName);

        await _signInManager.RefreshSignInAsync(user);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UpdateProfileAsync(ClaimsPrincipal userPrincipal, ProfileInfoDto model)
    {
        var user = await _userManager.GetUserAsync(userPrincipal);
        if (user == null) return Result.Failure("Usuario no encontrado.");

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.AccountSettings = model.AccountSettings;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<List<User>> GetAdminsToNotifyAsync(Expression<Func<AccountSettings, bool>> predicate)
    {
        var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
        var compiledPredicate = predicate.Compile();
        return allAdmins.Where(u => compiledPredicate(u.AccountSettings)).ToList();
    }

    /// <inheritdoc />
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _userManager.Users.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<UserDto>>> GetAllUsersForManagementAsync()
    {
        try
        {
            var users = await _userManager.Users.OrderBy(u => u.FirstName).ToListAsync();
            var userDtos = new List<UserDto>();
            var now = DateTime.UtcNow;
            const int inactivityDaysThreshold = 30;

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isAdmin = roles.Contains("Admin");

                var userDto = new UserDto
                {
                    Id = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email,
                    Role = isAdmin ? "Administrador" : "Usuario Estándar",
                    RegisteredDate = user.CreatedAt.ToLocalTime()
                };

                if (isAdmin && user.LastLoginAt.HasValue)
                {
                    var daysInactive = (now - user.LastLoginAt.Value).TotalDays;
                    if (daysInactive > inactivityDaysThreshold) userDto.IsDeletableByInactivity = true;
                }

                userDtos.Add(userDto);
            }

            return Result.Success<IEnumerable<UserDto>>(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la lista de usuarios para gestión.");
            return Result.Failure<IEnumerable<UserDto>>("Ocurrió un error al cargar los usuarios.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> PromoteToAdminAsync(int userIdToPromote)
    {
        var user = await _userManager.FindByIdAsync(userIdToPromote.ToString());
        if (user == null) return Result.Failure("Usuario no encontrado.");

        var isAlreadyAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (isAlreadyAdmin)
            return Result.Success();

        var result = await _userManager.AddToRoleAsync(user, "Admin");

        if (result.Succeeded)
        {
            _logger.LogInformation("Usuario {UserId} ha sido ascendido a Administrador.", userIdToPromote);
            return Result.Success();
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return Result.Failure($"No se pudo ascender al usuario: {errors}");
    }

    /// <inheritdoc />
    public async Task<Result> DeleteUserAsync(int userIdToDelete, int currentUserId)
    {
        // Regla de Seguridad: Un administrador no puede eliminarse a sí mismo.
        if (userIdToDelete == currentUserId)
            return Result.Failure("No puedes eliminar tu propia cuenta de administrador.");

        var userToDelete = await _userManager.FindByIdAsync(userIdToDelete.ToString());
        if (userToDelete == null)
            return Result.Success();

        // Regla de Seguridad: Un usuario estándar no puede eliminar a un administrador.
        var isUserAdmin = await _userManager.IsInRoleAsync(userToDelete, "Admin");
        if (isUserAdmin) return Result.Failure("No está permitido eliminar a un usuario administrador.");

        var emailOfDeletedUser = userToDelete.Email;
        var nameOfDeletedUser = userToDelete.FirstName;

        var result = await _userManager.DeleteAsync(userToDelete);

        if (result.Succeeded)
        {
            _logger.LogInformation("Usuario {UserId} ha sido eliminado por el administrador {AdminId}.", userIdToDelete,
                currentUserId);
            if (!string.IsNullOrEmpty(emailOfDeletedUser))
                await _alertService.SendAccountDeletedEmailAsync(emailOfDeletedUser, nameOfDeletedUser);
            return Result.Success();
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return Result.Failure($"No se pudo eliminar al usuario: {errors}");
    }

    /// <inheritdoc />
    public async Task<Result<string>> InitiateAdminDeletionAsync(int adminToDeleteId, int currentAdminId,
        string currentAdminPassword, IUrlHelper urlHelper, string scheme)
    {
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        if (admins.Count < 3)
            return Result.Failure<string>(
                "La eliminación de un administrador solo está permitida si existen al menos tres administradores.");

        if (adminToDeleteId == currentAdminId)
            return Result.Failure<string>("No puedes eliminar tu propia cuenta.");

        var currentAdmin = await _userManager.FindByIdAsync(currentAdminId.ToString());
        if (currentAdmin == null || !await _userManager.CheckPasswordAsync(currentAdmin, currentAdminPassword))
            return Result.Failure<string>("Tu contraseña es incorrecta. La acción ha sido cancelada.");

        var adminToDelete = await _userManager.FindByIdAsync(adminToDeleteId.ToString());
        if (adminToDelete == null)
            return Result.Failure<string>("El administrador que intentas eliminar no fue encontrado.");

        // Generar un token de un solo uso con un propósito específico para la eliminación.
        var tokenProvider = "Default";
        var purpose = $"delete-admin:{adminToDeleteId}";
        var token = await _userManager.GenerateUserTokenAsync(adminToDelete, tokenProvider, purpose);

        var confirmationLink = urlHelper.Action("ConfirmDeletion", "UserManagement",
            new { id = adminToDeleteId, token }, scheme);

        if (string.IsNullOrEmpty(confirmationLink))
            return Result.Failure<string>("No se pudo generar el enlace de confirmación.");

        var otherAdmins = admins.Where(a => a.Id != currentAdminId && a.Id != adminToDeleteId).ToList();

        await _alertService.SendAdminDeletionRequestEmailAsync(otherAdmins, currentAdmin.FirstName,
            adminToDelete.FirstName, confirmationLink);

        return Result.Success(adminToDelete.FirstName);
    }

    /// <inheritdoc />
    public async Task<Result> ConfirmAdminDeletionAsync(int adminToDeleteId, string token)
    {
        var adminToDelete = await _userManager.FindByIdAsync(adminToDeleteId.ToString());
        if (adminToDelete == null)
            return Result.Failure("El administrador a eliminar ya no existe.");

        // Validar el token de confirmación con su propósito específico.
        var tokenProvider = "Default";
        var purpose = $"delete-admin:{adminToDeleteId}";
        var isTokenValid = await _userManager.VerifyUserTokenAsync(adminToDelete, tokenProvider, purpose, token);

        if (!isTokenValid)
            return Result.Failure("El enlace de confirmación no es válido o ha expirado.");

        var emailOfDeletedAdmin = adminToDelete.Email;
        var nameOfDeletedAdmin = adminToDelete.FirstName;

        var result = await _userManager.DeleteAsync(adminToDelete);

        if (result.Succeeded)
        {
            _logger.LogWarning("El administrador {AdminToDeleteId} ha sido eliminado tras confirmación.",
                adminToDeleteId);

            if (!string.IsNullOrEmpty(emailOfDeletedAdmin))
                await _alertService.SendAccountDeletedEmailAsync(emailOfDeletedAdmin, nameOfDeletedAdmin);
            return Result.Success();
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return Result.Failure($"No se pudo eliminar al administrador: {errors}");
    }
}