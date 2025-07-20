using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
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
        try
        {
            var newInvitation = new InvitationCode
            {
                // Genera un código único y difícil de adivinar
                Code = Guid.NewGuid().ToString("N"),
                // Define una fecha de expiración, por ejemplo, 7 días a partir de ahora
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsAdmin = isAdmin, // Asigna el rol según el parámetro 
                CreatedByUserId = createdByUserId,
                IsUsed = false
            };

            _context.InvitationCodes.Add(newInvitation);
            await _context.SaveChangesAsync();

            await _alertService.SendInvitationEmailAsync(email, "Nuevo Usuario", newInvitation);
            return Result.Success(newInvitation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear y enviar el código de invitación.");
            return Result.Failure<InvitationCode>("Error interno al procesar la invitación.");
        }
    }

    public async Task<Result<InvitationCode>> ValidateCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<InvitationCode>("El código de invitación no puede estar vacío.");

        var invitation = await _context.InvitationCodes
            .FirstOrDefaultAsync(c => c.Code == code);

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