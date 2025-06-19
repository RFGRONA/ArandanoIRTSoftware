using Microsoft.Extensions.Options;
using Supabase; // Supabase.Client
// Result
// SupabaseSettings, TokenSettings
// DeviceDataModel, DeviceActivationModel, DeviceTokenModel, StatusModel
using ArandanoIRT.Web.Common;
using ArandanoIRT.Web.Configuration;
using ArandanoIRT.Web.Data.DTOs.DeviceApi;
using ArandanoIRT.Web.Data.Models;
using ArandanoIRT.Web.Services.Contracts;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Responses; // Para ModeledResponse

namespace ArandanoIRT.Web.Services.Implementation;

public class DeviceService : IDeviceService
{
    private readonly Client _supabaseClient; // Supabase.Client
    private readonly SupabaseSettings _supabaseSettings;
    private readonly TokenSettings _tokenSettings;
    private readonly ILogger<DeviceService> _logger;
    private readonly IDataSubmissionService _dataSubmissionService;

    // Constantes para nombres de estados (idealmente, se obtendrían de la DB o una enum)
    private const string StatusNameActive = "ACTIVE";
    private const string StatusNamePendingActivation = "PENDING_ACTIVATION";
    private const string StatusNameExpired = "EXPIRED";
    private const string StatusNameRevoked = "REVOKED";

    public DeviceService(
        Client supabaseClient,
        IOptions<SupabaseSettings> supabaseSettingsOptions,
        IOptions<TokenSettings> tokenSettingsOptions,
        ILogger<DeviceService> logger,
        IDataSubmissionService dataSubmissionService)
    {
        _supabaseClient = supabaseClient;
        _supabaseSettings = supabaseSettingsOptions.Value;
        _tokenSettings = tokenSettingsOptions.Value;
        _logger = logger;
        _dataSubmissionService = dataSubmissionService;
    }

    private Supabase.Interfaces.ISupabaseTable<T, Supabase.Realtime.RealtimeChannel> GetTable<T>()
        where T : BaseModel, new()
    {
        // Ejemplo de cómo podrías querer configurar headers por defecto para la ServiceRoleKey
        // Esto depende de si quieres hacerlo en cada llamada o una vez.
        // Por ahora, asumiremos que los headers se añaden en cada llamada específica si es necesario
        // o que el cliente se configura con ellos si el SDK lo permite globalmente de forma sencilla.
        // Para las operaciones CRUD, el cliente de Supabase suele manejar la autenticación si está configurado.
        // La ServiceRoleKey puede ser necesaria si el RLS está muy restrictivo.
        return _supabaseClient.From<T>();
    }

    // Helper para obtener el ID de un estado por su nombre
    private async Task<Result<int>> GetStatusIdAsync(string statusName)
    {
        try
        {
            var response = await GetTable<StatusModel>()
                .Filter("name", Supabase.Postgrest.Constants.Operator.Equals, statusName)
                .Single(); // Esperamos un único resultado

            if (response == null)
            {
                _logger.LogError("Estado '{StatusName}' no encontrado en la base de datos.", statusName);
                return Result.Failure<int>($"Estado '{statusName}' no configurado.");
            }

            return Result.Success(response.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el ID del estado '{StatusName}'.", statusName);
            return Result.Failure<int>($"Error interno al buscar estado: {ex.Message}");
        }
    }

    public async Task<Result<DeviceActivationResponseDto>> ActivateDeviceAsync(
        DeviceActivationRequestDto activationRequest)
    {
        _logger.LogInformation(
            "Intentando activar dispositivo con DeviceId: {DeviceId}, ActivationCode: {ActivationCode}, MAC: {MacAddress}",
            activationRequest.DeviceId, activationRequest.ActivationCode, activationRequest.MacAddress);

        if (activationRequest.DeviceId <= 0)
        {
            _logger.LogWarning("ID de dispositivo inválido en la solicitud de activación: {DeviceId}", activationRequest.DeviceId);
            return Result.Failure<DeviceActivationResponseDto>("El ID de dispositivo proporcionado es inválido.");
        }

        if (string.IsNullOrWhiteSpace(activationRequest.MacAddress))
        {
            _logger.LogWarning("MAC Address no proporcionada en la solicitud de activación para DeviceId: {DeviceId}", activationRequest.DeviceId);
            return Result.Failure<DeviceActivationResponseDto>("La dirección MAC es requerida.");
            // Considerar validación de formato de MAC Address aquí o con DataAnnotations en el DTO.
        }

        // 1. Obtener los IDs de los estados necesarios
        var pendingActivationStatusResult = await GetStatusIdAsync(StatusNamePendingActivation);
        var activeStatusResult = await GetStatusIdAsync(StatusNameActive);
        var expiredStatusResult = await GetStatusIdAsync(StatusNameExpired);
        // revokedStatusId se obtiene dentro de GenerateAndSaveNewTokensAsync si es necesario,
        // o podemos pasarlo si refactorizamos esa parte. El GenerateAndSaveNewTokensAsync actual lo obtiene.

        if (pendingActivationStatusResult.IsFailure) return Result.Failure<DeviceActivationResponseDto>(pendingActivationStatusResult.ErrorMessage);
        if (activeStatusResult.IsFailure) return Result.Failure<DeviceActivationResponseDto>(activeStatusResult.ErrorMessage);
        if (expiredStatusResult.IsFailure) return Result.Failure<DeviceActivationResponseDto>(expiredStatusResult.ErrorMessage);

        int pendingActivationStatusId = pendingActivationStatusResult.Value;
        int activeStatusId = activeStatusResult.Value;
        int expiredStatusId = expiredStatusResult.Value;

        try
        {
            // PASO 1: BÚSQUEDA INICIAL Y VALIDACIÓN DE MAC GLOBAL
            _logger.LogDebug("Validando MAC Address globalmente: {MacAddress}", activationRequest.MacAddress);
            var deviceWithMac = await GetTable<DeviceDataModel>()
                .Filter("mac_address", Supabase.Postgrest.Constants.Operator.Equals, activationRequest.MacAddress)
                .Single();

            if (deviceWithMac != null && deviceWithMac.Id != activationRequest.DeviceId)
            {
                _logger.LogWarning(
                    "Intento de activación para DeviceId: {RequestedDeviceId} con MAC: {MacAddress} que ya está registrada para DeviceId: {ExistingDeviceId}",
                    activationRequest.DeviceId, activationRequest.MacAddress, deviceWithMac.Id);
                return Result.Failure<DeviceActivationResponseDto>("La dirección MAC ya está registrada para otro dispositivo.");
            }

            // PASO 2: BÚSQUEDA DEL CÓDIGO DE ACTIVACIÓN PENDIENTE
            _logger.LogDebug(
                "Buscando registro de activación PENDIENTE para DeviceId: {DeviceId}, Code: {ActivationCode}",
                activationRequest.DeviceId, activationRequest.ActivationCode);
            var activationRecord = await GetTable<DeviceActivationModel>()
                .Filter("device_id", Supabase.Postgrest.Constants.Operator.Equals, activationRequest.DeviceId.ToString())
                .Filter("activation_code", Supabase.Postgrest.Constants.Operator.Equals, activationRequest.ActivationCode)
                .Filter("status_id", Supabase.Postgrest.Constants.Operator.Equals, pendingActivationStatusId.ToString())
                .Single();

            // CASO A: CÓDIGO DE ACTIVACIÓN PENDIENTE ENCONTRADO (ACTIVACIÓN NUEVA)
            if (activationRecord != null)
            {
                _logger.LogInformation(
                    "Código de activación PENDIENTE encontrado (ID: {ActivationId}) para DeviceId: {DeviceId}. Procediendo como activación nueva.",
                    activationRecord.Id, activationRequest.DeviceId);

                if (activationRecord.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning(
                        "El código de activación PENDIENTE ID: {ActivationId} para DeviceId: {DeviceId} ha expirado (ExpiresAt: {ExpiresAt}).",
                        activationRecord.Id, activationRequest.DeviceId, activationRecord.ExpiresAt);
                    activationRecord.StatusId = expiredStatusId; // Marcar como expirado en la DB
                    await GetTable<DeviceActivationModel>().Update(activationRecord);
                    return Result.Failure<DeviceActivationResponseDto>("El código de activación ha expirado.");
                }

                var deviceData = await GetTable<DeviceDataModel>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, activationRequest.DeviceId.ToString())
                    .Single();

                if (deviceData == null)
                {
                    _logger.LogError(
                        "CRÍTICO: No se encontró DeviceData para DeviceId: {DeviceId} asociado a un código de activación PENDIENTE válido (ID: {ActivationId}). Inconsistencia.",
                        activationRequest.DeviceId, activationRecord.Id);
                    return Result.Failure<DeviceActivationResponseDto>("Error interno crítico: no se encontraron datos del dispositivo.");
                }

                // Validar y asignar MAC Address a DeviceData
                if (!string.IsNullOrWhiteSpace(deviceData.MacAddress) && deviceData.MacAddress != activationRequest.MacAddress)
                {
                    _logger.LogWarning(
                        "Conflicto de MAC para DeviceId: {DeviceId} durante activación nueva. MAC existente: {ExistingMac}, MAC solicitada: {RequestedMac}. Se deniega la activación.",
                        deviceData.Id, deviceData.MacAddress, activationRequest.MacAddress);
                    // Esto podría pasar si la MAC fue asignada por otro medio o si deviceWithMac falló en detectarlo (improbable si la TX es atómica, pero Postgrest no garantiza TX entre llamadas).
                    // O si la validación global de MAC no fue suficiente (ej. MAC es null en DB pero se intenta cambiar).
                    // Por la lógica de deviceWithMac, este caso es menos probable si deviceData.MacAddress no es null.
                    // El caso más común aquí es que deviceData.MacAddress ES null/vacío.
                    return Result.Failure<DeviceActivationResponseDto>("El dispositivo ya tiene una dirección MAC diferente asignada.");
                }
                
                // Asignar MAC si está vacía o es la misma (para idempotencia si se reintenta antes de completar)
                if (string.IsNullOrWhiteSpace(deviceData.MacAddress) || deviceData.MacAddress == activationRequest.MacAddress)
                {
                    if (deviceData.MacAddress != activationRequest.MacAddress) // Solo actualizar si es diferente y la actual es null/empty
                    {
                        deviceData.MacAddress = activationRequest.MacAddress;
                        var updateDeviceDataResponse = await GetTable<DeviceDataModel>().Update(deviceData);
                        if (updateDeviceDataResponse.ResponseMessage?.IsSuccessStatusCode != true)
                        {
                            _logger.LogError(
                                "Error al actualizar DeviceData con MAC Address para DeviceId: {DeviceId}. Response: {Response}",
                                deviceData.Id, updateDeviceDataResponse.ResponseMessage?.ReasonPhrase);
                            return Result.Failure<DeviceActivationResponseDto>("Error al guardar la dirección MAC del dispositivo.");
                        }
                        _logger.LogInformation("MAC Address {MacAddress} asignada y guardada para DeviceId: {DeviceId}.", deviceData.MacAddress, deviceData.Id);
                    }
                }
                else // Esto cubre el caso donde deviceData.MacAddress no es null/empty Y NO es igual a activationRequest.MacAddress
                {
                     _logger.LogWarning(
                        "Intento de cambiar MAC Address en DeviceId: {DeviceId} de {ExistingMac} a {RequestedMac} durante activación nueva denegado.",
                        deviceData.Id, deviceData.MacAddress, activationRequest.MacAddress);
                    return Result.Failure<DeviceActivationResponseDto>("No se puede cambiar la MAC Address de un dispositivo ya provisionado con una MAC diferente.");
                }


                // Actualizar device_activation: status_id a ACTIVE, activated_at = NOW().
                activationRecord.StatusId = activeStatusId;
                activationRecord.ActivatedAt = DateTime.UtcNow;
                var updateActivationResponse = await GetTable<DeviceActivationModel>().Update(activationRecord);
                if (updateActivationResponse.ResponseMessage?.IsSuccessStatusCode != true)
                {
                    _logger.LogError(
                        "Error al actualizar el estado del registro de activación ID: {ActivationId}. Response: {Response}",
                        activationRecord.Id, updateActivationResponse.ResponseMessage?.ReasonPhrase);
                    return Result.Failure<DeviceActivationResponseDto>("Error al procesar la activación (actualización de código fallida).");
                }
                _logger.LogInformation("Registro de activación ID: {ActivationId} actualizado a ACTIVO.", activationRecord.Id);

                // Generar y guardar nuevos tokens. GenerateAndSaveNewTokensAsync se encarga de revocar los antiguos.
                var tokenGenerationResult = await GenerateAndSaveNewTokensAsync(deviceData.Id, null); // oldRefreshTokenToRevoke = null
                if (tokenGenerationResult.IsFailure)
                {
                    return Result.Failure<DeviceActivationResponseDto>(tokenGenerationResult.ErrorMessage);
                }

                var responseDto = new DeviceActivationResponseDto
                {
                    AccessToken = tokenGenerationResult.Value.newAccessToken,
                    RefreshToken = tokenGenerationResult.Value.newRefreshToken,
                    AccessTokenExpiration = tokenGenerationResult.Value.newAccessTokenExpiration,
                    DataCollectionTime = deviceData.DataCollectionTimeMinutes
                };
                _logger.LogInformation("Activación nueva completada para DeviceId: {DeviceId}.", deviceData.Id);
                return Result.Success(responseDto);
            }
            // CASO B: CÓDIGO DE ACTIVACIÓN PENDIENTE NO ENCONTRADO (POSIBLE RE-ACTIVACIÓN/RECUPERACIÓN)
            else
            {
                _logger.LogInformation(
                    "Código de activación PENDIENTE no encontrado para DeviceId: {DeviceId} y Code: {ActivationCode}. Verificando para re-activación/recuperación.",
                    activationRequest.DeviceId, activationRequest.ActivationCode);

                var deviceData = await GetTable<DeviceDataModel>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, activationRequest.DeviceId.ToString())
                    .Single();

                if (deviceData == null)
                {
                    _logger.LogWarning(
                        "Intento de re-activación para DeviceId: {DeviceId} que no existe.",
                        activationRequest.DeviceId);
                    return Result.Failure<DeviceActivationResponseDto>("Código de activación inválido, no encontrado, o ya utilizado."); // Mensaje genérico para no dar pistas
                }

                // Validar MAC Address almacenada
                if (string.IsNullOrWhiteSpace(deviceData.MacAddress) || deviceData.MacAddress != activationRequest.MacAddress)
                {
                    _logger.LogWarning(
                        "Intento de re-activación para DeviceId: {DeviceId}. MAC recibida ({ReceivedMac}) no coincide con MAC almacenada ({StoredMac}) o la almacenada es nula/vacía.",
                        activationRequest.DeviceId, activationRequest.MacAddress, deviceData.MacAddress);
                    return Result.Failure<DeviceActivationResponseDto>("La dirección MAC no coincide con la registrada para este dispositivo o el dispositivo no completó su activación inicial con una MAC.");
                }

                // Verificar el código de activación original (sin importar su estado actual, pero debe existir)
                var originalActivationRecord = await GetTable<DeviceActivationModel>()
                    .Filter("device_id", Supabase.Postgrest.Constants.Operator.Equals, activationRequest.DeviceId.ToString())
                    .Filter("activation_code", Supabase.Postgrest.Constants.Operator.Equals, activationRequest.ActivationCode)
                    .Single();

                if (originalActivationRecord == null)
                {
                    _logger.LogWarning(
                        "Intento de re-activación para DeviceId: {DeviceId}. El código de activación ({ActivationCode}) no corresponde al original de este dispositivo (MAC: {MacAddress}).",
                        activationRequest.DeviceId, activationRequest.ActivationCode, deviceData.MacAddress);
                    return Result.Failure<DeviceActivationResponseDto>("Código de activación inválido para re-activación.");
                }

                // Si MAC y código original coinciden, es una re-activación legítima
                _logger.LogInformation(
                    "Re-activación legítima detectada para DeviceId: {DeviceId} con MAC: {MacAddress} y código original ID: {OriginalActivationId}. Generando nuevos tokens.",
                    deviceData.Id, deviceData.MacAddress, originalActivationRecord.Id);

                // Generar y guardar nuevos tokens. GenerateAndSaveNewTokensAsync se encarga de revocar los antiguos.
                var tokenGenerationResult = await GenerateAndSaveNewTokensAsync(deviceData.Id, null); // oldRefreshTokenToRevoke = null
                if (tokenGenerationResult.IsFailure)
                {
                    return Result.Failure<DeviceActivationResponseDto>(tokenGenerationResult.ErrorMessage);
                }

                // Generar Log de Advertencia para la re-activación
                var warningLogEntry = new DeviceLogEntryDto
                {
                    LogType = "WARNING",
                    LogMessage = $"Device re-activation successful for DeviceId: {deviceData.Id} with matching MAC address ({deviceData.MacAddress}). Previous NVS data might have been lost.",
                    // InternalDeviceTemperature y InternalDeviceHumidity serán null por defecto en el DTO
                };
                // Necesitamos un DeviceIdentityContext para SaveDeviceLogAsync
                var deviceContextForLog = new DeviceIdentityContext { DeviceId = deviceData.Id, PlantId = deviceData.PlantId ?? 0, CropId = deviceData.CropId ?? 0 };
                
                var logResult = await _dataSubmissionService.SaveDeviceLogAsync(deviceContextForLog, warningLogEntry);
                if (logResult.IsFailure)
                {
                    // No fallar la activación por esto, solo loggear la falla del log.
                    _logger.LogWarning(
                        "No se pudo guardar el log de advertencia para la re-activación del DeviceId: {DeviceId}. Error: {ErrorMessage}",
                        deviceData.Id, logResult.ErrorMessage);
                } else {
                    _logger.LogInformation("Log de advertencia por re-activación guardado para DeviceId: {DeviceId}.", deviceData.Id);
                }

                var responseDto = new DeviceActivationResponseDto
                {
                    AccessToken = tokenGenerationResult.Value.newAccessToken,
                    RefreshToken = tokenGenerationResult.Value.newRefreshToken,
                    AccessTokenExpiration = tokenGenerationResult.Value.newAccessTokenExpiration,
                    DataCollectionTime = deviceData.DataCollectionTimeMinutes
                };
                _logger.LogInformation("Re-activación completada para DeviceId: {DeviceId}.", deviceData.Id);
                return Result.Success(responseDto);
            }
        }
        catch (PostgrestException pgEx)
        {
            _logger.LogError(pgEx, "Error de Postgrest durante la activación del DeviceId {DeviceIdFromRequest} con MAC {MacAddress}",
                 activationRequest.DeviceId, activationRequest.MacAddress);
            return Result.Failure<DeviceActivationResponseDto>($"Error de base de datos durante la activación: {pgEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada durante la activación del DeviceId: {DeviceIdFromRequest} con MAC {MacAddress}",
                activationRequest.DeviceId, activationRequest.MacAddress);
            return Result.Failure<DeviceActivationResponseDto>($"Error interno del servidor durante la activación: {ex.Message}");
        }
    }

    public async Task<Result<DeviceAuthResponseDto>> AuthenticateDeviceAsync(string accessToken)
    {
        _logger.LogInformation("Autenticando dispositivo con Access Token (prefijo): {AccessTokenPrefix}",
            accessToken.Substring(0, Math.Min(10, accessToken.Length)));

        var validationResult = await ValidateTokenAndGetDeviceDetailsAsync(accessToken);
        if (validationResult.IsFailure)
        {
            // Si ValidateTokenAndGetDeviceDetailsAsync falla (ej. token expirado, inválido),
            // entonces AuthenticateDeviceAsync también falla con el mismo error.
            _logger.LogWarning("Validación de token fallida durante la autenticación: {ErrorMessage}",
                validationResult.ErrorMessage);
            return Result.Failure<DeviceAuthResponseDto>(validationResult.ErrorMessage);
        }

        var deviceDetails = validationResult.Value;

        if (deviceDetails.RequiresTokenRefresh)
        {
            _logger.LogInformation("Token para DeviceId {DeviceId} requiere refresco. Generando nuevos tokens.",
                deviceDetails.DeviceId);

            // Obtener el refresh token actual asociado al access token que se está validando (o al dispositivo)
            // Esto es un poco circular si solo tenemos el access token. Asumimos que el refresh token
            // que vamos a revocar es el que está actualmente activo para este deviceId.
            // Si el cliente tiene el refresh token, debería usar /refresh-token.
            // Aquí, si refrescamos, necesitamos revocar el refresh token *actual* asociado con este dispositivo.
            var currentActiveTokenForDevice = await GetTable<DeviceTokenModel>()
                .Filter("device_id", Supabase.Postgrest.Constants.Operator.Equals, deviceDetails.DeviceId.ToString())
                .Filter("access_token", Supabase.Postgrest.Constants.Operator.Equals,
                    accessToken) // Asegurarnos que es el token que se usó
                .Filter("status_id", Supabase.Postgrest.Constants.Operator.Equals,
                    (await GetStatusIdAsync(StatusNameActive)).Value.ToString())
                .Single();

            string? refreshTokenToRevoke = currentActiveTokenForDevice?.RefreshToken;

            var tokenGenerationResult =
                await GenerateAndSaveNewTokensAsync(deviceDetails.DeviceId, refreshTokenToRevoke);
            if (tokenGenerationResult.IsFailure)
            {
                return Result.Failure<DeviceAuthResponseDto>(tokenGenerationResult.ErrorMessage);
            }

            return Result.Success(new DeviceAuthResponseDto
            {
                AccessToken = tokenGenerationResult.Value.newAccessToken,
                RefreshToken = tokenGenerationResult.Value.newRefreshToken,
                AccessTokenExpiration = tokenGenerationResult.Value.newAccessTokenExpiration,
                DataCollectionTime = (int)deviceDetails.DataCollectionTimeMinutes // Cast de short a int
            });
        }

        // Si el token es válido y no requiere refresco, devolver info con tokens actuales.
        // Necesitamos el RefreshToken actual asociado.
        var currentTokenRecord = await GetTable<DeviceTokenModel>()
            .Filter("access_token", Supabase.Postgrest.Constants.Operator.Equals, accessToken)
            .Filter("status_id", Supabase.Postgrest.Constants.Operator.Equals,
                (await GetStatusIdAsync(StatusNameActive)).Value.ToString())
            .Single();

        if (currentTokenRecord == null)
        {
            // Esto no debería pasar si ValidateTokenAndGetDeviceDetailsAsync tuvo éxito.
            _logger.LogError(
                "Inconsistencia: Token activo no encontrado para {AccessTokenPrefix} después de una validación exitosa.",
                accessToken.Substring(0, Math.Min(10, accessToken.Length)));
            return Result.Failure<DeviceAuthResponseDto>("Error interno del servidor al recuperar detalles del token.");
        }

        _logger.LogInformation(
            "Autenticación exitosa para DeviceId {DeviceId}. No se requiere refresco inmediato de token.",
            deviceDetails.DeviceId);
        return Result.Success(new DeviceAuthResponseDto
        {
            AccessToken = accessToken, // El mismo token de acceso que se validó
            RefreshToken = currentTokenRecord.RefreshToken, // El refresh token asociado
            AccessTokenExpiration = currentTokenRecord.AccessTokenExpiresAt,
            DataCollectionTime = (int)deviceDetails.DataCollectionTimeMinutes
        });
    }

    public async Task<Result<DeviceAuthResponseDto>> RefreshDeviceTokenAsync(string refreshTokenValue)
    {
        _logger.LogInformation("Intentando refrescar usando Refresh Token (prefijo): {RefreshTokenPrefix}",
            refreshTokenValue.Substring(0, Math.Min(10, refreshTokenValue.Length)));

        var activeStatusResult = await GetStatusIdAsync(StatusNameActive);
        if (activeStatusResult.IsFailure) return Result.Failure<DeviceAuthResponseDto>(activeStatusResult.ErrorMessage);
        int activeStatusId = activeStatusResult.Value;

        try
        {
            var tokenRecord = await GetTable<DeviceTokenModel>()
                .Filter("refresh_token", Supabase.Postgrest.Constants.Operator.Equals, refreshTokenValue)
                .Filter("status_id", Supabase.Postgrest.Constants.Operator.Equals, activeStatusId.ToString())
                .Single();

            if (tokenRecord == null)
            {
                _logger.LogWarning("Refresh Token no encontrado o no activo: {RefreshTokenPrefix}",
                    refreshTokenValue.Substring(0, Math.Min(10, refreshTokenValue.Length)));
                return Result.Failure<DeviceAuthResponseDto>("Refresh token inválido o no activo.");
            }

            if (tokenRecord.RefreshTokenExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "Refresh Token ID: {TokenId} para DeviceId: {DeviceId} ha expirado (ExpiresAt: {ExpiresAt}).",
                    tokenRecord.Id, tokenRecord.DeviceId, tokenRecord.RefreshTokenExpiresAt);
                // Opcional: Marcar este token como EXPIRADO en la DB.
                var expiredStatusResult = await GetStatusIdAsync(StatusNameExpired);
                if (expiredStatusResult.IsSuccess)
                {
                    tokenRecord.StatusId = expiredStatusResult.Value;
                    await GetTable<DeviceTokenModel>().Update(tokenRecord);
                }

                return Result.Failure<DeviceAuthResponseDto>("Refresh token expirado.");
            }

            // Obtener DeviceData para DataCollectionTimeMinutes
            var deviceData = await GetTable<DeviceDataModel>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, tokenRecord.DeviceId.ToString())
                .Single();

            if (deviceData == null)
            {
                _logger.LogError(
                    "CRÍTICO: No se encontró DeviceData para DeviceId: {DeviceId} asociado a un refresh token válido. Inconsistencia.",
                    tokenRecord.DeviceId);
                return Result.Failure<DeviceAuthResponseDto>(
                    "Error interno: datos del dispositivo asociados al token no encontrados.");
            }

            // Generar nuevos tokens y revocar el refresh token antiguo (pasándolo a GenerateAndSaveNewTokensAsync)
            var tokenGenerationResult =
                await GenerateAndSaveNewTokensAsync(tokenRecord.DeviceId, tokenRecord.RefreshToken);
            if (tokenGenerationResult.IsFailure)
            {
                return Result.Failure<DeviceAuthResponseDto>(tokenGenerationResult.ErrorMessage);
            }

            return Result.Success(new DeviceAuthResponseDto
            {
                AccessToken = tokenGenerationResult.Value.newAccessToken,
                RefreshToken = tokenGenerationResult.Value.newRefreshToken,
                AccessTokenExpiration = tokenGenerationResult.Value.newAccessTokenExpiration,
                DataCollectionTime = (int)deviceData.DataCollectionTimeMinutes
            });
        }
        catch (PostgrestException pgEx)
        {
            _logger.LogError(pgEx,
                "Error de Postgrest durante el refresco de token: {RefreshTokenPrefix}. Message: {ErrorMessage}",
                refreshTokenValue.Substring(0, Math.Min(10, refreshTokenValue.Length)), pgEx.Message);
            return Result.Failure<DeviceAuthResponseDto>(
                $"Error de base de datos durante el refresco de token: {pgEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada durante el refresco de token: {RefreshTokenPrefix}",
                refreshTokenValue.Substring(0, Math.Min(10, refreshTokenValue.Length)));
            return Result.Failure<DeviceAuthResponseDto>(
                $"Error interno del servidor durante el refresco de token: {ex.Message}");
        }
    }

    public async Task<Result<AuthenticatedDeviceDetailsDto>> ValidateTokenAndGetDeviceDetailsAsync(string accessToken)
    {
        _logger.LogDebug("Validando Access Token (primeros 10 chars): {AccessTokenPrefix}",
            accessToken.Substring(0, Math.Min(10, accessToken.Length)));

        var activeStatusResult = await GetStatusIdAsync(StatusNameActive);
        if (activeStatusResult.IsFailure)
        {
            return Result.Failure<AuthenticatedDeviceDetailsDto>(activeStatusResult.ErrorMessage);
        }
        int activeStatusId = activeStatusResult.Value;

        try
        {
            var tokenRecordResponse = await GetTable<DeviceTokenModel>()
                .Filter("access_token", Supabase.Postgrest.Constants.Operator.Equals, accessToken)
                .Filter("status_id", Supabase.Postgrest.Constants.Operator.Equals, activeStatusId.ToString())
                .Single();

            if (tokenRecordResponse == null)
            {
                _logger.LogWarning("Token de acceso no encontrado o no activo: {AccessTokenPrefix}",
                    accessToken.Substring(0, Math.Min(10, accessToken.Length)));
                return Result.Failure<AuthenticatedDeviceDetailsDto>("Token inválido o no activo.");
            }

            DateTime expiryTimeFromDb = tokenRecordResponse.AccessTokenExpiresAt;
            DateTime expiryTimeUtc;

            // Determinar el DateTimeKind y convertir a UTC si es necesario
            if (expiryTimeFromDb.Kind == DateTimeKind.Unspecified)
            {
                _logger.LogWarning("AccessTokenExpiresAt for token ID {TokenId} was Kind Unspecified ('{DbTime}'). " +
                                   "Converting from assumed server's local time to UTC.",
                                   tokenRecordResponse.Id, expiryTimeFromDb.ToString("o"));
                // Asume que la hora 'Unspecified' es la hora local del servidor donde se ejecuta el backend.
                expiryTimeUtc = TimeZoneInfo.ConvertTimeToUtc(expiryTimeFromDb, TimeZoneInfo.Local);
            }
            else if (expiryTimeFromDb.Kind == DateTimeKind.Local)
            {
                _logger.LogWarning("AccessTokenExpiresAt for token ID {TokenId} was Kind Local ('{DbTime}'). Converting to UTC.",
                                   tokenRecordResponse.Id, expiryTimeFromDb.ToString("o"));
                expiryTimeUtc = expiryTimeFromDb.ToUniversalTime();
            }
            else // expiryTimeFromDb.Kind == DateTimeKind.Utc
            {
                expiryTimeUtc = expiryTimeFromDb;
            }

            // Log para depuración con los valores procesados
            _logger.LogInformation(
                "Token Validation UTC Check: DeviceId={DeviceId}, TokenId={TokenId}, " +
                "DB.AccessTokenExpiresAt='{DbTime}' (Kind: {DbKind}), " +
                "Processed ExpiryUtc='{ProcessedExpiryUtc}', Current DateTime.UtcNow='{CurrentUtcNow}'",
                tokenRecordResponse.DeviceId,
                tokenRecordResponse.Id,
                expiryTimeFromDb.ToString("o"), 
                expiryTimeFromDb.Kind.ToString(),
                expiryTimeUtc.ToString("o"),
                DateTime.UtcNow.ToString("o")
            );

            if (expiryTimeUtc < DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "Token de acceso ID: {TokenId} para DeviceId: {DeviceId} ha expirado. " +
                    "Comparando Expiry (Processed UTC): {ExpiryUtcProcessed} con Current (UTC): {CurrentUtcNow}.",
                    tokenRecordResponse.Id,
                    tokenRecordResponse.DeviceId,
                    expiryTimeUtc.ToString("o"),
                    DateTime.UtcNow.ToString("o")
                );
                return Result.Failure<AuthenticatedDeviceDetailsDto>("Token expirado.");
            }

            // Obtener DeviceData para los detalles adicionales
            var deviceData = await GetTable<DeviceDataModel>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, tokenRecordResponse.DeviceId.ToString())
                .Single();

            if (deviceData == null)
            {
                _logger.LogError(
                    "CRÍTICO: No se encontró DeviceData para DeviceId: {DeviceId} asociado a un token válido. Inconsistencia de datos.",
                    tokenRecordResponse.DeviceId);
                return Result.Failure<AuthenticatedDeviceDetailsDto>(
                    "Error interno: datos del dispositivo asociados al token no encontrados.");
            }

            // Calcular si `access_token_expires_at` está cerca de expirar (usando expiryTimeUtc)
            bool requiresRefresh = expiryTimeUtc <
                                   DateTime.UtcNow.AddMinutes(_tokenSettings.AccessTokenNearExpiryThresholdMinutes);
            if (requiresRefresh)
            {
                _logger.LogInformation(
                    "Token de acceso para DeviceId: {DeviceId} está cerca de expirar y requiere refresco.",
                    deviceData.Id); // Correcto usar deviceData.Id aquí ya que es el ID del dispositivo.
            }

            var details = new AuthenticatedDeviceDetailsDto
            {
                DeviceId = deviceData.Id,
                PlantId = deviceData.PlantId ?? 0,
                CropId = deviceData.CropId ?? 0,
                DataCollectionTimeMinutes = deviceData.DataCollectionTimeMinutes,
                RequiresTokenRefresh = requiresRefresh
                // Roles ya se inicializa a ["Device"] en el DTO
            };

            _logger.LogInformation("Token de acceso validado exitosamente para DeviceId: {DeviceId}.",
                details.DeviceId);
            return Result.Success(details);
        }
        catch (PostgrestException pgEx)
        {
            _logger.LogError(pgEx,
                "Error de Postgrest durante la validación del token: {AccessTokenPrefix}. Message: {ErrorMessage}",
                accessToken.Substring(0, Math.Min(10, accessToken.Length)), pgEx.Message);
            return Result.Failure<AuthenticatedDeviceDetailsDto>(
                $"Error de base de datos durante la validación del token: {pgEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada durante la validación del token: {AccessTokenPrefix}",
                accessToken.Substring(0, Math.Min(10, accessToken.Length)));
            return Result.Failure<AuthenticatedDeviceDetailsDto>(
                $"Error interno del servidor durante la validación del token: {ex.Message}");
        }
    }

    private async Task<Result<(string newAccessToken, string newRefreshToken, DateTime newAccessTokenExpiration)>>  
        GenerateAndSaveNewTokensAsync(int deviceId, string? oldRefreshTokenToRevoke = null)
    {
        var activeStatusResult = await GetStatusIdAsync(StatusNameActive);
        var revokedStatusResult = await GetStatusIdAsync(StatusNameRevoked);

        if (activeStatusResult.IsFailure)
            return Result.Failure<(string, string, DateTime)>(activeStatusResult.ErrorMessage);
        if (revokedStatusResult.IsFailure)
            return Result.Failure<(string, string, DateTime)>(revokedStatusResult.ErrorMessage);

        int activeStatusId = activeStatusResult.Value;
        int revokedStatusId = revokedStatusResult.Value;

        // Si se proporcionó un oldRefreshTokenToRevoke, marcarlo como revocado.
        if (!string.IsNullOrEmpty(oldRefreshTokenToRevoke))
        {
            var tokenToRevokeResponse = await GetTable<DeviceTokenModel>()
                .Filter("refresh_token", Supabase.Postgrest.Constants.Operator.Equals, oldRefreshTokenToRevoke)
                .Filter("device_id", Supabase.Postgrest.Constants.Operator.Equals, deviceId.ToString())
                // .Filter("status_id", Postgrest.Constants.Operator.Equals, activeStatusId.ToString()) // Podría ya estar expirado, pero lo revocamos igual.
                .Get(); // Puede haber múltiples si algo salió mal, pero refresh tokens deben ser únicos por (device, value)

            if (tokenToRevokeResponse?.Models != null)
            {
                foreach (var tokenToRevoke in
                         tokenToRevokeResponse.Models.Where(t =>
                             t.StatusId == activeStatusId)) // Solo revocar los activos
                {
                    tokenToRevoke.StatusId = revokedStatusId;
                    tokenToRevoke.RevokedAt = DateTime.UtcNow;
                    await GetTable<DeviceTokenModel>().Update(tokenToRevoke);
                }
            }
        }

        // También revocar todos los Access Tokens activos para este dispositivo, ya que estamos generando uno nuevo.
        var existingAccessTokens = await GetTable<DeviceTokenModel>()
            .Filter("device_id", Supabase.Postgrest.Constants.Operator.Equals, deviceId.ToString())
            .Filter("status_id", Supabase.Postgrest.Constants.Operator.Equals, activeStatusId.ToString())
            .Get();

        if (existingAccessTokens?.Models != null)
        {
            foreach (var tokenToRevoke in existingAccessTokens.Models)
            {
                tokenToRevoke.StatusId = revokedStatusId;
                tokenToRevoke.RevokedAt = DateTime.UtcNow;
                await GetTable<DeviceTokenModel>().Update(tokenToRevoke);
            }
        }

        // --- Inicio de Lógica de Generación de Tokens ---
        int configuredDurationMinutes = _tokenSettings.AccessTokenDurationMinutes;
        int effectiveDurationMinutes;

        // Comprobación defensiva de la duración configurada.
        // Si es sospechosamente corta (ej. < 5 minutos pero no el default de 60 que esperamos, o es exactamente 0),
        // forzamos un valor mínimo para permitir pruebas y diagnosticar si la config es el problema.
        if (configuredDurationMinutes == 0)
        {
            _logger.LogWarning(
                "TokenSettings.AccessTokenDurationMinutes es 0 para DeviceId {DeviceId}. Usando override de 10 minutos. ¡VERIFICAR CONFIGURACIÓN!",
                deviceId);
            effectiveDurationMinutes = 10; // Un valor distinto al default de 60 para ver si esto se aplica.
        }
        else if (configuredDurationMinutes > 0 && configuredDurationMinutes < 5)
        {
            // Si es 1, 2, 3, 4
            _logger.LogWarning(
                "TokenSettings.AccessTokenDurationMinutes ({ConfigDuration} min) es muy corto para DeviceId {DeviceId}. Usando override de 10 minutos.",
                configuredDurationMinutes, deviceId);
            effectiveDurationMinutes = 10;
        }
        else if (configuredDurationMinutes < 0)
        {
            // Duración negativa
            _logger.LogError(
                "TokenSettings.AccessTokenDurationMinutes ({ConfigDuration} min) es NEGATIVA para DeviceId {DeviceId}. Usando override de 10 minutos. ¡ERROR CRÍTICO EN CONFIGURACIÓN!",
                configuredDurationMinutes, deviceId);
            effectiveDurationMinutes = 10;
        }
        else
        {
            effectiveDurationMinutes = configuredDurationMinutes; // Debería ser 60 si la config se carga bien.
        }

        var nowUtc = DateTime.UtcNow; // Captura la hora actual UNA VEZ.
        var newAccessToken = Guid.NewGuid().ToString("N");
        var newRefreshToken = Guid.NewGuid().ToString("N");

        // Calcula la expiración usando la duración efectiva y asegúrate de que sea UTC.
        var newAccessTokenExpiration =
            DateTime.SpecifyKind(nowUtc.AddMinutes(effectiveDurationMinutes), DateTimeKind.Utc);
        var newRefreshTokenExpiration =
            DateTime.SpecifyKind(nowUtc.AddDays(_tokenSettings.RefreshTokenDurationDays), DateTimeKind.Utc);

        // (Opcional, pero recomendado si puedes ver logs de alguna forma, aunque sea después del hecho)
        _logger.LogInformation(
            "Generando token para DeviceId {DeviceId}: NowUtc={NowUtc}, ConfiguredDuration={ConfiguredDuration}min, EffectiveDuration={EffectiveDuration}min, Calculated AccessTokenExpiresAt={CalculatedExpiry}",
            deviceId,
            nowUtc,
            configuredDurationMinutes,
            effectiveDurationMinutes,
            newAccessTokenExpiration
        );

        var newDeviceTokenRecord = new DeviceTokenModel
        {
            DeviceId = deviceId,
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = newAccessTokenExpiration, // Fecha UTC explícita
            RefreshTokenExpiresAt = newRefreshTokenExpiration, // Fecha UTC explícita
            StatusId = activeStatusId,
            CreatedAt = nowUtc // Usar la misma 'nowUtc' capturada
        };

        var insertResult = await GetTable<DeviceTokenModel>().Insert(newDeviceTokenRecord);
        if (insertResult?.Models == null || !insertResult.Models.Any())
        {
            _logger.LogError("Error al insertar nuevos tokens para DeviceId {DeviceId}. Response: {Response}", deviceId,
                insertResult?.ResponseMessage?.ReasonPhrase);
            return Result.Failure<(string, string, DateTime)>("Error al guardar nuevos tokens.");
        }

        _logger.LogInformation(
            "Nuevos tokens generados y guardados para DeviceId {DeviceId}. AccessTokenExpiresAt: {Expiry}", deviceId,
            newAccessTokenExpiration);
        return Result.Success((newAccessToken, newRefreshToken, newAccessTokenExpiration));
        // --- Fin de Lógica de Generación de Tokens ---
    } 
}