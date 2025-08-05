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

public class UserService : IUserService
{
    private readonly IAlertService _alertService;
    private readonly ApplicationDbContext _context;
    private readonly IInvitationService _invitationService;
    private readonly ILogger<UserService> _logger;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

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

    public async Task<(SignInResult Result, bool JustLockedOut)> LoginUserAsync(LoginDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, lockoutOnFailure: true);
            return (result, false); // No hay usuario, no se puede bloquear
        }
        // 1. Verificamos si el usuario está a UN intento de ser bloqueado.
        //    Usamos la configuración de Identity en lugar de un número fijo (5).
        var isAboutToLockOut = user.AccessFailedCount == _userManager.Options.Lockout.MaxFailedAccessAttempts - 1;

        // 2. Realizamos el intento de login.
        var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, true, true);

        // 3. Si el intento resultó en un bloqueo Y sabíamos que estaba a punto de ocurrir, enviamos la alerta.
        if (isAboutToLockOut && signInResult.IsLockedOut)
        {
            _logger.LogWarning("La cuenta para {Email} ha sido bloqueada en este intento.", user.Email);
            return (signInResult, true);
        }

        return (signInResult, false);
    }

    public async Task<Result> RegisterUserAsync(RegisterDto model)
    {
        // 1. Validar la invitación primero (operación de solo lectura)
        var invitationResult = await _invitationService.ValidateCodeAsync(model.InvitationCode);
        if (invitationResult.IsFailure) return Result.Failure(invitationResult.ErrorMessage);
        var invitation = invitationResult.Value;

        // 2. Crear el usuario
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
            // Si es admin, establecemos sus valores por defecto específicos.
            user.AccountSettings.EmailOnHelpRequest = true;
            user.AccountSettings.EmailOnAppFailureAlert = true;
            user.AccountSettings.EmailOnDeviceFailureAlert = true;
            user.AccountSettings.EmailOnDeviceInactivity = true;
        }

        // UserManager.CreateAsync ya guarda el usuario en la BD.
        var identityResult = await _userManager.CreateAsync(user, model.Password);

        if (!identityResult.Succeeded)
        {
            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        _logger.LogInformation("Usuario {Email} creado en la base de datos.", user.Email);

        // 3. Intentar las operaciones secundarias (asignar rol, marcar código)
        try
        {
            if (invitation.IsAdmin)
            {
                if (!await _roleManager.RoleExistsAsync("Admin"))
                    await _roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            // Marcar el código como usado (ahora es una operación separada)
            await _invitationService.MarkCodeAsUsedAsync(invitation.Id);
        }
        catch (Exception ex)
        {
            // Si algo falla DESPUÉS de crear el usuario, debemos deshacerlo.
            _logger.LogError(ex,
                "Error al asignar rol o marcar invitación para el usuario {Email}. Revirtiendo creación.", user.Email);
            await _userManager.DeleteAsync(user);
            return Result.Failure("Ocurrió un error al finalizar el registro. Por favor, intente de nuevo.");
        }

        // 4. Si todo salió bien, iniciar sesión
        await _signInManager.SignInAsync(user, false);
        return Result.Success(user.Id);
    }

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

    public async Task<Result<(string Name, string ResetLink)>> GeneratePasswordResetAsync(ForgotPasswordDto model,
        IUrlHelper urlHelper, string scheme)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        // NOTA DE SEGURIDAD: Si el usuario no se encuentra, no devolvemos un error.
        // Simplemente terminamos el proceso silenciosamente. Esto previene que un atacante
        // pueda usar este formulario para descubrir qué correos están registrados en el sistema.
        if (user == null)
            // Devolvemos un Result exitoso pero con valores vacíos. El controlador no enviará correo.
            return Result.Success(("", ""));

        // Generar el token de reseteo
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Generar la URL de callback que irá en el correo
        var callbackUrl = urlHelper.Action(
            "ResetPassword",
            "Account",
            new { token, email = user.Email },
            scheme);

        if (string.IsNullOrEmpty(callbackUrl))
            return Result.Failure<(string, string)>("No se pudo generar la URL de reseteo.");

        return Result.Success((user.FirstName, callbackUrl));
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            // No revelamos que el usuario no existe por seguridad.
            return Result.Failure("Ocurrió un error. Por favor, intente de nuevo.");

        // El método ResetPasswordAsync valida el token y actualiza la contraseña.
        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        // Notificar al usuario que su contraseña ha cambiado
        await _alertService.SendPasswordChangedEmailAsync(user.Email, user.FirstName);

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(ClaimsPrincipal userPrincipal, ChangePasswordDto model)
    {
        var user = await _userManager.GetUserAsync(userPrincipal);
        if (user == null) return Result.Failure("Usuario no encontrado.");

        // El método ChangePasswordAsync valida la contraseña antigua y establece la nueva.
        var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        // Notificar al usuario que su contraseña ha cambiado
        await _alertService.SendPasswordChangedEmailAsync(user.Email, user.FirstName);

        // Refrescar la cookie de sesión del usuario para actualizar el sello de seguridad
        await _signInManager.RefreshSignInAsync(user);

        return Result.Success();
    }

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

    public async Task<List<User>> GetAdminsToNotifyAsync(Expression<Func<AccountSettings, bool>> predicate)
    {
        var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
        var compiledPredicate = predicate.Compile();
        return allAdmins.Where(u => compiledPredicate(u.AccountSettings)).ToList();
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _userManager.Users.ToListAsync();
    }
}