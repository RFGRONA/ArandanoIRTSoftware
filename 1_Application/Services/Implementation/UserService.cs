using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class UserService : IUserService
{
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
        ILogger<UserService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _invitationService = invitationService;
        _logger = logger;
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
    if (invitationResult.IsFailure)
    {
        return Result.Failure(invitationResult.ErrorMessage);
    }
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
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
            }
            await _userManager.AddToRoleAsync(user, "Admin");
        }

        // Marcar el código como usado (ahora es una operación separada)
        await _invitationService.MarkCodeAsUsedAsync(invitation.Id);
    }
    catch (Exception ex)
    {
        // Si algo falla DESPUÉS de crear el usuario, debemos deshacerlo.
        _logger.LogError(ex, "Error al asignar rol o marcar invitación para el usuario {Email}. Revirtiendo creación.", user.Email);
        await _userManager.DeleteAsync(user); // Elimina al usuario recién creado
        return Result.Failure("Ocurrió un error al finalizar el registro. Por favor, intente de nuevo.");
    }

    // 4. Si todo salió bien, iniciar sesión
    await _signInManager.SignInAsync(user, isPersistent: false);
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
}