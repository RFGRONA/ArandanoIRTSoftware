using Microsoft.Extensions.Options;
using Supabase;
using ArandanoIRT.Web.Common;
using ArandanoIRT.Web.Configuration;
using ArandanoIRT.Web.Data.DTOs.Admin;
using ArandanoIRT.Web.Data.Models;
using ArandanoIRT.Web.Services.Contracts;
using Microsoft.AspNetCore.Mvc.Rendering;
using Supabase.Postgrest.Models;

namespace ArandanoIRT.Web.Services.Implementation;

public class DeviceAdminService : IDeviceAdminService
{
    private readonly Client _supabaseClient;
    private readonly ILogger<DeviceAdminService> _logger;
    private readonly TokenSettings _tokenSettings; // Para la duración de expiración del código de activación

    // Constantes para nombres de estados (ya definidos en DeviceService, idealmente compartidos)
    private const string StatusNameActive = "ACTIVE";
    private const string StatusNamePendingActivation = "PENDING_ACTIVATION";
    // private const string StatusNameInactive = "INACTIVE"; // Si lo usas para dispositivos

    public DeviceAdminService(Client supabaseClient, IOptions<TokenSettings> tokenSettingsOptions, ILogger<DeviceAdminService> logger)
    {
        _supabaseClient = supabaseClient;
        _tokenSettings = tokenSettingsOptions.Value; // Podríamos crear una ActivationCodeSettings si es diferente
        _logger = logger;
    }

    private Supabase.Interfaces.ISupabaseTable<T, Supabase.Realtime.RealtimeChannel> GetTable<T>() where T : BaseModel, new() => _supabaseClient.From<T>();


    // Helper para obtener el ID de un estado por su nombre (duplicado de DeviceService, podría refactorizarse)
    private async Task<Result<int>> GetStatusIdAsync(string statusName)
    {
        try
        {
            var response = await GetTable<StatusModel>()
                .Filter("name", Supabase.Postgrest.Constants.Operator.Equals, statusName)
                .Single();

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


    public async Task<Result<DeviceCreationResultDto>> CreateDeviceAsync(DeviceCreateDto deviceDto)
    {
        try
        {
            var newDevice = new DeviceDataModel
            {
                Name = deviceDto.Name,
                Description = deviceDto.Description,
                PlantId = deviceDto.PlantId,
                DataCollectionTimeMinutes = deviceDto.DataCollectionTimeMinutes,
                StatusId = deviceDto.StatusId, // El estado del dispositivo en sí
                RegisteredAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
                // CropId se puede inferir de PlantId si es necesario o guardar también.
                // Por ahora, la tabla plant_data ya tiene CropId.
            };

            // Obtener CropId de la planta seleccionada para guardarlo en DeviceData también
            if (deviceDto.PlantId > 0)
            {
                var plant = await _supabaseClient.From<PlantDataModel>()
                                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, deviceDto.PlantId.ToString())
                                    .Single();
                if (plant != null) newDevice.CropId = plant.CropId;
            }


            var deviceResponse = await GetTable<DeviceDataModel>().Insert(newDevice);

            if (deviceResponse?.Models == null || !deviceResponse.Models.Any())
            {
                _logger.LogError("Error al crear el dispositivo. Supabase response: {@response}", deviceResponse);
                return Result.Failure<DeviceCreationResultDto>("No se pudo crear el dispositivo.");
            }
            var createdDevice = deviceResponse.Models.First();
            _logger.LogInformation("Dispositivo creado con ID: {DeviceId}", createdDevice.Id);

            // Ahora, generar y guardar el DeviceActivation
            var pendingStatusResult = await GetStatusIdAsync(StatusNamePendingActivation);
            if (pendingStatusResult.IsFailure)
            {
                // Aunque el dispositivo se creó, la activación no se puede generar.
                // Se podría loguear y devolver un éxito parcial o intentar revertir/marcar el dispositivo.
                // Por ahora, logueamos y devolvemos un error para la operación completa.
                _logger.LogError("No se pudo obtener el estado PENDING_ACTIVATION para DeviceActivation. Error: {Error}", pendingStatusResult.ErrorMessage);
                return Result.Failure<DeviceCreationResultDto>("Dispositivo creado, pero falló la generación del código de activación: " + pendingStatusResult.ErrorMessage);
            }

            var activationCode = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant(); // Código más corto
            var expiresAt = DateTime.UtcNow.AddDays(7); // Código de activación expira en 7 días (configurable)

            var newDeviceActivation = new DeviceActivationModel
            {
                DeviceId = createdDevice.Id,
                ActivationCode = activationCode,
                StatusId = pendingStatusResult.Value,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            var activationResponse = await GetTable<DeviceActivationModel>().Insert(newDeviceActivation);
            if (activationResponse?.Models == null || !activationResponse.Models.Any())
            {
                _logger.LogError("Dispositivo creado (ID: {DeviceId}), pero falló al guardar DeviceActivation. Supabase response: {@response}", createdDevice.Id, activationResponse);
                return Result.Failure<DeviceCreationResultDto>("Dispositivo creado, pero no se pudo guardar el código de activación.");
            }

            _logger.LogInformation("DeviceActivation creado para DeviceID {DeviceId} con código {ActivationCode}", createdDevice.Id, activationCode);

            return Result.Success(new DeviceCreationResultDto
            {
                DeviceId = createdDevice.Id,
                ActivationCode = activationCode,
                ActivationCodeExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al crear dispositivo: {DeviceName}", deviceDto.Name);
            return Result.Failure<DeviceCreationResultDto>($"Error interno: {ex.Message}");
        }
    }
    public async Task<Result<IEnumerable<DeviceSummaryDto>>> GetAllDevicesAsync()
    {
        try
        {
            // Esta consulta es compleja por los joins implícitos.
            // Haremos varias consultas para construir el DTO.
            var deviceResponse = await GetTable<DeviceDataModel>().Get();
            if (deviceResponse?.Models == null)
                return Result.Success<IEnumerable<DeviceSummaryDto>>(new List<DeviceSummaryDto>());

            var devices = deviceResponse.Models;
            var summaries = new List<DeviceSummaryDto>();

            // Cache local para evitar N+1 en DB
            var plantsMap = new Dictionary<int, PlantDataModel>();
            var cropsMap = new Dictionary<int, CropModel>();
            var deviceStatusMap = new Dictionary<int, string>();
            var activationStatusMap = new Dictionary<int, string>(); // Para el status de DeviceActivation
            var deviceActivationsMap = new Dictionary<int, DeviceActivationModel>();


            var plantIds = devices.Where(d => d.PlantId.HasValue).Select(d => d.PlantId!.Value).Distinct().ToList();
            if (plantIds.Any())
            {
                var plants = await _supabaseClient.From<PlantDataModel>().Filter("id", Supabase.Postgrest.Constants.Operator.In, plantIds).Get();
                if (plants?.Models != null) plantsMap = plants.Models.ToDictionary(p => p.Id);

                var cropIdsFromPlants = plantsMap.Values.Where(p => p.CropId.HasValue).Select(p => p.CropId!.Value).Distinct().ToList();
                if (cropIdsFromPlants.Any())
                {
                    var crops = await _supabaseClient.From<CropModel>().Filter("id", Supabase.Postgrest.Constants.Operator.In, cropIdsFromPlants).Get();
                    if (crops?.Models != null) cropsMap = crops.Models.ToDictionary(c => c.Id, c => c);
                }
            }

            var deviceStatusIds = devices.Select(d => d.StatusId).Distinct().ToList();
            if (deviceStatusIds.Any())
            {
                var statuses = await GetTable<StatusModel>().Filter("id", Supabase.Postgrest.Constants.Operator.In, deviceStatusIds).Get();
                if (statuses?.Models != null) deviceStatusMap = statuses.Models.ToDictionary(s => s.Id, s => s.Name);
            }

            // Obtener DeviceActivations para los dispositivos
            var deviceIds = devices.Select(d => d.Id).ToList();
            if(deviceIds.Any())
            {
                var activations = await GetTable<DeviceActivationModel>()
                    .Filter("device_id", Supabase.Postgrest.Constants.Operator.In, deviceIds)
                    // Podríamos ordenar para obtener la más reciente si un dispositivo pudiera tener varias (no debería activas)
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending) 
                    .Get();

                if(activations?.Models != null)
                {
                    // Quedarnos con la activación más reciente por device_id
                    deviceActivationsMap = activations.Models
                        .GroupBy(a => a.DeviceId)
                        .ToDictionary(g => g.Key, g => g.First()); // Asume que la primera es la más relevante (por el Order)

                    var activationStatusIds = deviceActivationsMap.Values.Select(da => da.StatusId).Distinct().ToList();
                    if (activationStatusIds.Any())
                    {
                         var statuses = await GetTable<StatusModel>().Filter("id", Supabase.Postgrest.Constants.Operator.In, activationStatusIds).Get();
                         if (statuses?.Models != null) activationStatusMap = statuses.Models.ToDictionary(s => s.Id, s => s.Name);
                    }
                }
            }


            foreach (var device in devices)
            {
                PlantDataModel? plant = null;
                CropModel? crop = null;
                if (device.PlantId.HasValue)
                {
                    plantsMap.TryGetValue(device.PlantId.Value, out plant);
                    if (plant?.CropId.HasValue == true)
                    {
                        cropsMap.TryGetValue(plant.CropId.Value, out crop);
                    }
                }

                string activationStatusName = "N/A";
                if(deviceActivationsMap.TryGetValue(device.Id, out var activation))
                {
                    if(activationStatusMap.TryGetValue(activation.StatusId, out var actStatus))
                    {
                        activationStatusName = actStatus;
                    }
                }


                summaries.Add(new DeviceSummaryDto
                {
                    Id = device.Id,
                    Name = device.Name,
                    PlantName = plant?.Name ?? "N/A",
                    CropName = crop?.Name ?? "N/A",
                    DeviceStatusName = deviceStatusMap.TryGetValue(device.StatusId, out var ds) ? ds : "N/A",
                    ActivationStatusName = activationStatusName,
                    RegisteredAt = device.RegisteredAt.ToLocalTime()
                });
            }
            return Result.Success<IEnumerable<DeviceSummaryDto>>(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener todos los dispositivos.");
            return Result.Failure<IEnumerable<DeviceSummaryDto>>($"Error interno: {ex.Message}");
        }
    }
    public async Task<Result<DeviceDetailsDto?>> GetDeviceByIdAsync(int deviceId)
    {
        try
        {
            var device = await GetTable<DeviceDataModel>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, deviceId.ToString())
                .Single();

            if (device == null) return Result.Success<DeviceDetailsDto?>(null);

            PlantDataModel? plant = null;
            CropModel? crop = null;
            if (device.PlantId.HasValue)
            {
                plant = await _supabaseClient.From<PlantDataModel>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, device.PlantId.Value.ToString()).Single();
                if (plant?.CropId.HasValue == true)
                {
                    crop = await _supabaseClient.From<CropModel>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, plant.CropId.Value.ToString()).Single();
                }
            }
            var deviceStatus = await GetTable<StatusModel>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, device.StatusId.ToString()).Single();

            // Obtener el DeviceActivation más reciente (generalmente solo habrá uno relevante)
            DeviceActivationModel? activation = null;
            var activationResponse = await GetTable<DeviceActivationModel>()
                                       .Filter("device_id", Supabase.Postgrest.Constants.Operator.Equals, deviceId.ToString())
                                       .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                                       .Limit(1)
                                       .Get(); // Get() devuelve una lista
            if (activationResponse?.Models != null && activationResponse.Models.Any())
            {
                activation = activationResponse.Models.First();
            }

            string? activationStatusName = null;
            if (activation != null)
            {
                var actStatus = await GetTable<StatusModel>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, activation.StatusId.ToString()).Single();
                activationStatusName = actStatus?.Name;
            }

            var details = new DeviceDetailsDto
            {
                Id = device.Id,
                Name = device.Name,
                Description = device.Description,
                PlantName = plant?.Name ?? "N/A",
                CropName = crop?.Name ?? "N/A",
                CropCityName = crop?.CityName ?? "N/A",
                DataCollectionTimeMinutes = device.DataCollectionTimeMinutes,
                DeviceStatusName = deviceStatus?.Name ?? "N/A",
                RegisteredAt = device.RegisteredAt.ToLocalTime(),
                UpdatedAt = device.UpdatedAt.ToLocalTime(),
                ActivationId = activation?.Id,
                ActivationCode = activation?.ActivationCode,
                ActivationStatusName = activationStatusName,
                ActivationCodeExpiresAt = activation?.ExpiresAt.ToLocalTime(),
                DeviceActivatedAt = activation?.ActivatedAt?.ToLocalTime()
            };
            return Result.Success<DeviceDetailsDto?>(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener dispositivo ID: {DeviceId}", deviceId);
            return Result.Failure<DeviceDetailsDto?>($"Error interno: {ex.Message}");
        }
    }

    public async Task<Result<DeviceEditDto?>> GetDeviceForEditByIdAsync(int deviceId)
    {
        try
        {
            var device = await GetTable<DeviceDataModel>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, deviceId.ToString())
                .Single();

            if (device == null) return Result.Success<DeviceEditDto?>(null);

            return Result.Success<DeviceEditDto?>(new DeviceEditDto
            {
                Id = device.Id,
                Name = device.Name,
                Description = device.Description,
                PlantId = device.PlantId ?? 0,
                DataCollectionTimeMinutes = device.DataCollectionTimeMinutes,
                StatusId = device.StatusId,
                AvailablePlants = await GetPlantsForSelectionAsync(),
                AvailableStatuses = await GetDeviceStatusesForSelectionAsync()
            });
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Excepción al obtener dispositivo para editar ID: {DeviceId}", deviceId);
            return Result.Failure<DeviceEditDto?>($"Error interno: {ex.Message}");
        }
    }

    public async Task<Result> UpdateDeviceAsync(DeviceEditDto deviceDto)
    {
        try
        {
            var existingDevice = await GetTable<DeviceDataModel>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, deviceDto.Id.ToString())
                .Single();
            if (existingDevice == null) return Result.Failure("Dispositivo no encontrado para actualizar.");

            existingDevice.Name = deviceDto.Name;
            existingDevice.Description = deviceDto.Description;
            existingDevice.PlantId = deviceDto.PlantId;
            existingDevice.DataCollectionTimeMinutes = deviceDto.DataCollectionTimeMinutes;
            existingDevice.StatusId = deviceDto.StatusId;

            // Actualizar CropId si PlantId cambió
            if (deviceDto.PlantId > 0)
            {
                var plant = await _supabaseClient.From<PlantDataModel>()
                                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, deviceDto.PlantId.ToString())
                                    .Single();
                existingDevice.CropId = plant?.CropId;
            } else {
                existingDevice.CropId = null;
            }


            var response = await GetTable<DeviceDataModel>().Update(existingDevice);
            if (response?.ResponseMessage?.IsSuccessStatusCode == true)
            {
                _logger.LogInformation("Dispositivo ID: {DeviceId} actualizado.", deviceDto.Id);
                return Result.Success();
            }
            _logger.LogError("Error al actualizar dispositivo ID {DeviceId}. Supabase response: {@response}", deviceDto.Id, response);
            return Result.Failure("No se pudo actualizar el dispositivo.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al actualizar dispositivo ID: {DeviceId}", deviceDto.Id);
            return Result.Failure($"Error interno: {ex.Message}");
        }
    }

    public async Task<Result> DeleteDeviceAsync(int deviceId)
    {
        try
        {
            // DeviceActivation y DeviceToken tienen ON DELETE CASCADE para device_id.
            // DeviceLog, SensorData, ThermalData también tienen ON DELETE CASCADE.
            // Así que borrar el DeviceData debería limpiar todo lo asociado.
            await GetTable<DeviceDataModel>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, deviceId.ToString()).Delete();
            _logger.LogInformation("Solicitud de eliminación para dispositivo ID: {DeviceId} enviada.", deviceId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al eliminar dispositivo ID: {DeviceId}", deviceId);
            return Result.Failure($"Error interno: {ex.Message}");
        }
    }

    public async Task<IEnumerable<SelectListItem>> GetPlantsForSelectionAsync()
    {
        try
        {
            // Para mostrar PlantName (CropName)
            var plantResponse = await _supabaseClient.From<PlantDataModel>().Get();
            if (plantResponse?.Models == null) return new List<SelectListItem>();

            var plants = plantResponse.Models;
            var cropIds = plants.Where(p => p.CropId.HasValue).Select(p => p.CropId!.Value).Distinct().ToList();
            var cropsMap = new Dictionary<int, string>();
            if (cropIds.Any())
            {
                var crops = await _supabaseClient.From<CropModel>().Filter("id", Supabase.Postgrest.Constants.Operator.In, cropIds).Get();
                if (crops?.Models != null) cropsMap = crops.Models.ToDictionary(cr => cr.Id, cr => cr.Name);
            }

            return plants.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Name} (Cultivo: {(p.CropId.HasValue && cropsMap.TryGetValue(p.CropId.Value, out var cn) ? cn : "N/A")})"
            }).OrderBy(sli => sli.Text).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lista de plantas para selección.");
            return new List<SelectListItem>();
        }
    }

    public async Task<IEnumerable<SelectListItem>> GetDeviceStatusesForSelectionAsync()
    {
        try
        {
            // Podrías filtrar por status relevantes para DeviceData si es necesario.
            var response = await GetTable<StatusModel>().Order("name", Supabase.Postgrest.Constants.Ordering.Ascending).Get();
            if (response?.Models != null)
            {
                return response.Models
                    .Where(s => s.Name == StatusNameActive || s.Name == "INACTIVE" || s.Name == "MAINTENANCE") // Filtrar por estados relevantes para dispositivos
                    .Select(s => new SelectListItem
                    {
                        Value = s.Id.ToString(),
                        Text = s.Name
                    }).ToList();
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error al obtener lista de estados de dispositivo para selección.");
        }
        return new List<SelectListItem>();
    }
}