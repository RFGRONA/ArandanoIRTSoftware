using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class DeviceService : IDeviceService
{
    private readonly ApplicationDbContext _context;
    private readonly TokenSettings _tokenSettings;
    private readonly ILogger<DeviceService> _logger;
    // IDataSubmissionService ya no es necesario aquí porque los logs los maneja ILogger.

    public DeviceService(
        ApplicationDbContext context,
        IOptions<TokenSettings> tokenSettingsOptions,
        ILogger<DeviceService> logger)
    {
        _context = context;
        _tokenSettings = tokenSettingsOptions.Value;
        _logger = logger;
    }

    public async Task<Result<DeviceActivationResponseDto>> ActivateDeviceAsync(DeviceActivationRequestDto activationRequest)
    {
        _logger.LogInformation("Inicio de activación para DeviceId: {DeviceId}, MAC: {MacAddress}", activationRequest.DeviceId, activationRequest.MacAddress);

        if (string.IsNullOrWhiteSpace(activationRequest.MacAddress))
        {
            return Result.Failure<DeviceActivationResponseDto>("La dirección MAC es requerida.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Validar que la MAC no esté registrada para OTRO dispositivo
            var deviceWithMac = await _context.Devices
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.MacAddress == activationRequest.MacAddress);

            if (deviceWithMac != null && deviceWithMac.Id != activationRequest.DeviceId)
            {
                _logger.LogWarning("Intento de activación con MAC {MacAddress} que ya pertenece al DeviceId {ExistingDeviceId}",
                    activationRequest.MacAddress, deviceWithMac.Id);
                return Result.Failure<DeviceActivationResponseDto>("La dirección MAC ya está registrada para otro dispositivo.");
            }

            // 2. Buscar el dispositivo y su código de activación PENDIENTE
            var device = await _context.Devices
                .Include(d => d.DeviceActivations)
                .FirstOrDefaultAsync(d => d.Id == activationRequest.DeviceId);

            if (device == null)
            {
                return Result.Failure<DeviceActivationResponseDto>("Dispositivo no encontrado.");
            }

            var activationRecord = device.DeviceActivations
                .FirstOrDefault(a => a.ActivationCode == activationRequest.ActivationCode && a.Status == ActivationStatus.PENDING);

            // --- CASO A: ACTIVACIÓN NUEVA (Código PENDIENTE encontrado) ---
            if (activationRecord != null)
            {
                _logger.LogInformation("Código PENDIENTE encontrado (ID: {ActivationId}). Procediendo como activación nueva.", activationRecord.Id);

                if (activationRecord.ExpiresAt < DateTime.UtcNow)
                {
                    activationRecord.Status = ActivationStatus.EXPIRED;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Result.Failure<DeviceActivationResponseDto>("El código de activación ha expirado.");
                }

                // Asignar MAC y cambiar estado del dispositivo a ACTIVO
                device.MacAddress = activationRequest.MacAddress;
                device.Status = DeviceStatus.ACTIVE;
                device.UpdatedAt = DateTime.UtcNow;

                // Cambiar estado del código de activación a COMPLETADO
                activationRecord.Status = ActivationStatus.COMPLETED;
                activationRecord.ActivatedAt = DateTime.UtcNow;

                // Generar tokens
                var tokenResult = await GenerateAndSaveNewTokensAsync(device.Id);
                if (tokenResult.IsFailure) throw new Exception(tokenResult.ErrorMessage); // Forzará el rollback

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Activación nueva completada para DeviceId: {DeviceId}", device.Id);
                return Result.Success(new DeviceActivationResponseDto
                {
                    AccessToken = tokenResult.Value.NewAccessToken,
                    RefreshToken = tokenResult.Value.NewRefreshToken,
                    AccessTokenExpiration = tokenResult.Value.NewAccessTokenExpiration,
                    DataCollectionTime = device.DataCollectionIntervalMinutes
                });
            }
            // --- CASO B: RE-ACTIVACIÓN (Código PENDIENTE no encontrado) ---
            else
            {
                _logger.LogInformation("Código PENDIENTE no encontrado. Verificando para re-activación.");

                // Validar que la MAC coincida y que el código de activación pertenezca a este dispositivo (aunque ya no esté pendiente)
                bool isMacValid = device.MacAddress == activationRequest.MacAddress;
                bool isCodeValid = device.DeviceActivations.Any(a => a.ActivationCode == activationRequest.ActivationCode);

                if (!isMacValid || !isCodeValid)
                {
                    _logger.LogWarning("Fallo de re-activación para DeviceId {DeviceId}. MAC Válida: {IsMacValid}, Código Válido: {IsCodeValid}",
                        device.Id, isMacValid, isCodeValid);
                    return Result.Failure<DeviceActivationResponseDto>("Código de activación o MAC inválidos para re-activación.");
                }

                _logger.LogInformation("Re-activación legítima detectada para DeviceId: {DeviceId}. Generando nuevos tokens.", device.Id);

                // Si es una re-activación, el estado del dispositivo ya debería ser ACTIVO, no lo cambiamos.
                device.UpdatedAt = DateTime.UtcNow;

                var tokenResult = await GenerateAndSaveNewTokensAsync(device.Id);
                if (tokenResult.IsFailure) throw new Exception(tokenResult.ErrorMessage);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Loguear que hubo una re-activación
                _logger.LogWarning("Dispositivo {DeviceId} ha sido re-activado exitosamente con su código original y MAC Address.", device.Id);

                return Result.Success(new DeviceActivationResponseDto
                {
                    AccessToken = tokenResult.Value.NewAccessToken,
                    RefreshToken = tokenResult.Value.NewRefreshToken,
                    AccessTokenExpiration = tokenResult.Value.NewAccessTokenExpiration,
                    DataCollectionTime = device.DataCollectionIntervalMinutes
                });
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Excepción durante el proceso de activación para DeviceId {DeviceId}", activationRequest.DeviceId);
            return Result.Failure<DeviceActivationResponseDto>($"Error interno del servidor durante la activación: {ex.Message}");
        }
    }

    public async Task<Result<DeviceAuthResponseDto>> RefreshDeviceTokenAsync(string refreshTokenValue)
    {
        _logger.LogInformation("Intentando refrescar token.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var tokenRecord = await _context.DeviceTokens
                .FirstOrDefaultAsync(t => t.RefreshToken == refreshTokenValue && t.Status == TokenStatus.ACTIVE);

            if (tokenRecord == null)
            {
                return Result.Failure<DeviceAuthResponseDto>("Refresh token inválido o no activo.");
            }

            if (tokenRecord.RefreshTokenExpiresAt < DateTime.UtcNow)
            {
                tokenRecord.Status = TokenStatus.REVOKED; // Podríamos tener un estado EXPIRED también
                tokenRecord.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Result.Failure<DeviceAuthResponseDto>("Refresh token expirado.");
            }

            var device = await _context.Devices.FindAsync(tokenRecord.DeviceId);
            if (device == null)
            {
                return Result.Failure<DeviceAuthResponseDto>("Dispositivo asociado al token no encontrado.");
            }

            var tokenResult = await GenerateAndSaveNewTokensAsync(tokenRecord.DeviceId);
            if (tokenResult.IsFailure) throw new Exception(tokenResult.ErrorMessage);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success(new DeviceAuthResponseDto
            {
                AccessToken = tokenResult.Value.NewAccessToken,
                RefreshToken = tokenResult.Value.NewRefreshToken,
                AccessTokenExpiration = tokenResult.Value.NewAccessTokenExpiration,
                DataCollectionTime = device.DataCollectionIntervalMinutes
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Excepción durante el refresco de token.");
            return Result.Failure<DeviceAuthResponseDto>($"Error interno del servidor: {ex.Message}");
        }
    }

    public async Task<Result<AuthenticatedDeviceDetailsDto>> ValidateTokenAndGetDeviceDetailsAsync(string accessToken)
    {
        _logger.LogDebug("Validando Access Token.");
        try
        {
            var tokenRecord = await _context.DeviceTokens
                .AsNoTracking()
                .Include(t => t.Device) // Incluimos el dispositivo para no hacer otra consulta
                .FirstOrDefaultAsync(t => t.AccessToken == accessToken && t.Status == TokenStatus.ACTIVE);

            if (tokenRecord == null)
            {
                return Result.Failure<AuthenticatedDeviceDetailsDto>("Token inválido o no activo.");
            }

            if (tokenRecord.AccessTokenExpiresAt < DateTime.UtcNow)
            {
                // No se actualiza el estado aquí para no interferir con la transacción de refresco.
                // El cliente debe manejar el error y llamar a /refresh-token.
                return Result.Failure<AuthenticatedDeviceDetailsDto>("Token expirado.");
            }

            var device = tokenRecord.Device;
            if (device == null)
            {
                return Result.Failure<AuthenticatedDeviceDetailsDto>("Dispositivo asociado al token no encontrado.");
            }

            bool requiresRefresh = tokenRecord.AccessTokenExpiresAt < DateTime.UtcNow.AddMinutes(_tokenSettings.AccessTokenNearExpiryThresholdMinutes);

            var details = new AuthenticatedDeviceDetailsDto
            {
                DeviceId = device.Id,
                PlantId = device.PlantId ?? 0,
                CropId = device.CropId,
                DataCollectionTimeMinutes = device.DataCollectionIntervalMinutes,
                RequiresTokenRefresh = requiresRefresh
            };

            return Result.Success(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción durante la validación del token.");
            return Result.Failure<AuthenticatedDeviceDetailsDto>($"Error interno del servidor: {ex.Message}");
        }
    }

    private async Task<Result<(string NewAccessToken, string NewRefreshToken, DateTime NewAccessTokenExpiration)>>
        GenerateAndSaveNewTokensAsync(int deviceId)
    {
        // 1. Revocar todos los tokens ACTIVOS existentes para este dispositivo
        var existingTokens = await _context.DeviceTokens
            .Where(t => t.DeviceId == deviceId && t.Status == TokenStatus.ACTIVE)
            .ToListAsync();

        foreach (var token in existingTokens)
        {
            token.Status = TokenStatus.REVOKED;
            token.RevokedAt = DateTime.UtcNow;
        }

        // 2. Generar nuevos tokens y fechas de expiración
        var newAccessToken = Guid.NewGuid().ToString("N");
        var newRefreshToken = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var newAccessTokenExpiration = now.AddMinutes(_tokenSettings.AccessTokenDurationMinutes);
        var newRefreshTokenExpiration = now.AddDays(_tokenSettings.RefreshTokenDurationDays);

        // 3. Crear el nuevo registro de token
        var newTokenRecord = new DeviceToken
        {
            DeviceId = deviceId,
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = newAccessTokenExpiration,
            RefreshTokenExpiresAt = newRefreshTokenExpiration,
            Status = TokenStatus.ACTIVE
        };

        _context.DeviceTokens.Add(newTokenRecord);

        return Result.Success((newAccessToken, newRefreshToken, newAccessTokenExpiration));
    }
}