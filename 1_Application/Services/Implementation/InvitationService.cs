using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.Helper;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class InvitationService : IInvitationService
{
    private readonly IAlertService _alertService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvitationService> _logger;

    public InvitationService(ApplicationDbContext context, IAlertService alertService,
        ILogger<InvitationService> logger)
    {
        _context = context;
        _alertService = alertService;
        _logger = logger;
    }

    public async Task<Result<InvitationCode>> CreateInvitationAsync(string email, bool isAdmin, int? createdByUserId)
    {
        var isFirstInvitation = !await _context.InvitationCodes.AnyAsync();

        // Si es la primera invitación y no está marcada como "Admin", devolvemos un error.
        if (isFirstInvitation && !isAdmin)
        {
            _logger.LogWarning("Intento de crear la primera invitación sin privilegios de administrador.");
            return Result.Failure<InvitationCode>("La primera invitación del sistema debe ser obligatoriamente para un rol de Administrador.");
        }
        
        try
        {
            // 1. Generar un código público, más corto y legible
            var publicCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            // 2. Generar el hash seguro que se guardará en la base de datos
            var hashedCode = SecurityHelper.GenerateInvitationHash(publicCode, email);
            
            var newInvitation = new InvitationCode
            {
                Code = hashedCode, // Guardamos el HASH en la BD
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsAdmin = isAdmin,
                CreatedByUserId = createdByUserId,
                IsUsed = false
            };

            _context.InvitationCodes.Add(newInvitation);
            await _context.SaveChangesAsync();

            // 3. Enviar el código PÚBLICO por correo, no el hash
            newInvitation.Code = publicCode; 
            await _alertService.SendInvitationEmailAsync(email, "Nuevo Usuario", newInvitation);
            
            return Result.Success(newInvitation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear y enviar el código de invitación.");
            return Result.Failure<InvitationCode>("Error interno al procesar la invitación.");
        }
    }

    public async Task<Result<InvitationCode>> ValidateCodeAsync(string code, string email)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<InvitationCode>("El código de invitación no puede estar vacío.");

        // 1. Recrear el hash a partir de los datos proporcionados por el usuario
        var hashedCodeToValidate = SecurityHelper.GenerateInvitationHash(code, email);

        // 2. Buscar el hash en la base de datos
        var invitation = await _context.InvitationCodes
            .FirstOrDefaultAsync(c => c.Code == hashedCodeToValidate);

        if (invitation == null) return Result.Failure<InvitationCode>("El código de invitación no es válido.");

        if (invitation.IsUsed) return Result.Failure<InvitationCode>("Este código de invitación ya ha sido utilizado.");

        if (invitation.ExpiresAt < DateTime.UtcNow)
            return Result.Failure<InvitationCode>("Este código de invitación ha expirado.");

        return Result.Success(invitation);
    }

    public async Task<Result> MarkCodeAsUsedAsync(int invitationId)
    {
        var invitation = await _context.InvitationCodes.FindAsync(invitationId);
        if (invitation == null) return Result.Failure("No se encontró la invitación para marcarla como usada.");

        invitation.IsUsed = true;
        await _context.SaveChangesAsync(); // Añadimos el guardado aquí

        return Result.Success();
    }
}