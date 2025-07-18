using System.Security.Claims;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IInvitationService _invitationService;
    private readonly ILogger<UserService> _logger;
    private readonly IRazorViewToStringRenderer _renderer;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;


    public UserService(
        ApplicationDbContext context,
        UserManager<User> userManager,
        RoleManager<ApplicationRole> roleManager,
        SignInManager<User> signInManager,
        IInvitationService invitationService,
        IEmailService emailService,
        IRazorViewToStringRenderer renderer,
        ILogger<UserService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _invitationService = invitationService;
        _logger = logger;
        _emailService = emailService;
        _renderer = renderer;
    }

    public async Task<SignInResult> LoginUserAsync(LoginDto model)
    {
        // La lógica de login es simple: solo llama al SignInManager
        return await _signInManager.PasswordSignInAsync(model.Email, model.Password, true,
            true);
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
        var emailHtml =
            await _renderer.RenderViewToStringAsync("/Views/Shared/EmailTemplates/_PasswordChangedEmail.cshtml",
                user.FirstName);
        await _emailService.SendEmailAsync(user.Email, user.FirstName, "Tu Contraseña ha sido Cambiada", emailHtml);

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
        var emailHtml =
            await _renderer.RenderViewToStringAsync("/Views/Shared/EmailTemplates/_PasswordChangedEmail.cshtml",
                user.FirstName);
        await _emailService.SendEmailAsync(user.Email, user.FirstName, "Tu Contraseña ha sido Cambiada", emailHtml);

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

    public async Task<List<User>> GetAdminsToNotifyForHelpRequestsAsync()
    {
        // Obtenemos todos los usuarios que tienen el rol "Admin"
        var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");

        // Filtramos la lista para quedarnos solo con aquellos que tienen la preferencia activada
        return allAdmins.Where(u => u.AccountSettings.EmailOnHelpRequest).ToList();
    }
}