using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.DTOs.Device;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class DeviceAdminService : IDeviceAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DeviceAdminService> _logger;
    private readonly TokenSettings _tokenSettings;

    public DeviceAdminService(
        ApplicationDbContext context,
        IOptions<TokenSettings> tokenSettingsOptions,
        ILogger<DeviceAdminService> logger)
    {
        _context = context;
        _tokenSettings = tokenSettingsOptions.Value;
        _logger = logger;
    }

    public async Task<Result<DeviceCreationResultDto>> CreateDeviceAsync(DeviceCreateDto deviceDto)
    {
        // Usamos una transacción para asegurar que la creación del dispositivo y su código de activación
        // sea una operación atómica. O ambas tienen éxito, o ambas se revierten.
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var newDevice = new Device
            {
                Name = deviceDto.Name,
                MacAddress = deviceDto.MacAddress,
                Description = deviceDto.Description,
                PlantId = deviceDto.PlantId > 0 ? deviceDto.PlantId : null,
                DataCollectionIntervalMinutes = deviceDto.DataCollectionIntervalMinutes,
                Status = DeviceStatus.PENDING_ACTIVATION,
                RegisteredAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Asignar CropId basado en la planta seleccionada
            if (newDevice.PlantId.HasValue)
            {
                var plant = await _context.Plants.AsNoTracking().FirstOrDefaultAsync(p => p.Id == newDevice.PlantId.Value);
                if (plant != null)
                {
                    newDevice.CropId = plant.CropId;
                }
                else
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<DeviceCreationResultDto>("La planta seleccionada no existe.");
                }
            }
            else
            {
                await transaction.RollbackAsync();
                return Result.Failure<DeviceCreationResultDto>("El dispositivo debe estar asociado al menos a un cultivo.");
            }

            _context.Devices.Add(newDevice);
            await _context.SaveChangesAsync(); // Guardamos para obtener el ID del nuevo dispositivo

            _logger.LogInformation("Dispositivo creado con ID: {DeviceId}", newDevice.Id);

            // Generar y guardar el código de activación (DeviceActivation)
            var activationCode = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
            var expiresAt = DateTime.UtcNow.AddDays(_tokenSettings.ActivationCodeExpirationInDays);

            var newDeviceActivation = new DeviceActivation
            {
                DeviceId = newDevice.Id,
                ActivationCode = activationCode,
                Status = ActivationStatus.PENDING, // Usando el enum directamente
                ExpiresAt = expiresAt
            };

            _context.DeviceActivations.Add(newDeviceActivation);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync(); // Confirmamos todos los cambios a la DB

            _logger.LogInformation("DeviceActivation creado para DeviceID {DeviceId} con código {ActivationCode}", newDevice.Id, activationCode);

            return Result.Success(new DeviceCreationResultDto
            {
                DeviceId = newDevice.Id,
                ActivationCode = activationCode,
                ActivationCodeExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(); // Revertimos todo si algo falla
            _logger.LogError(ex, "Excepción al crear dispositivo: {DeviceName}", deviceDto.Name);
            return Result.Failure<DeviceCreationResultDto>($"Error interno al crear el dispositivo: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<DeviceSummaryDto>>> GetAllDevicesAsync()
    {
        try
        {
            // Consulta única y eficiente usando proyecciones
            var summaries = await _context.Devices
                .AsNoTracking()
                .AsQueryable()
                .OrderBy(d => d.Name)
                .Select(d => new DeviceSummaryDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    PlantName = d.Plant != null ? d.Plant.Name : "N/A",
                    CropName = d.Plant != null ? d.Plant.Crop.Name : (d.Crop != null ? d.Crop.Name : "N/A"),
                    DeviceStatus = d.Status,
                    ActivationStatus = d.DeviceActivations
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => (ActivationStatus?)a.Status)
                        .FirstOrDefault() ?? ActivationStatus.PENDING,
                    RegisteredAt = d.RegisteredAt
                })
                .ToListAsync<DeviceSummaryDto>();

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
            // Consulta única para obtener toda la información necesaria
            var details = await _context.Devices
                .AsNoTracking()
                .Where(d => d.Id == deviceId)
                .Select(d => new DeviceDetailsDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    MacAddress = d.MacAddress,
                    Description = d.Description,
                    PlantName = d.Plant != null ? d.Plant.Name : "N/A",
                    CropName = d.Plant != null ? d.Plant.Crop.Name : (d.Crop != null ? d.Crop.Name : "N/A"),
                    DataCollectionTimeMinutes = d.DataCollectionIntervalMinutes,
                    Status = d.Status,
                    RegisteredAt = d.RegisteredAt,
                    UpdatedAt = d.UpdatedAt,
                    ActivationDevices = d.DeviceActivations
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => new DeviceDetailsDto.DeviceActivationDetailsDto()
                        {
                            ActivationId = a.Id,
                            ActivationCode = a.ActivationCode,
                            ActivationStatus = a.Status,
                            ActivationCodeExpiresAt = a.ExpiresAt,
                            DeviceActivatedAt = a.ActivatedAt
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync<DeviceDetailsDto>();

            if (details == null)
            {
                return Result.Success<DeviceDetailsDto?>(null);
            }

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
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null) return Result.Success<DeviceEditDto?>(null);

            return Result.Success<DeviceEditDto?>(new DeviceEditDto
            {
                Id = device.Id,
                Name = device.Name,
                MacAddress = device.MacAddress,
                Description = device.Description,
                PlantId = device.PlantId ?? 0,
                DataCollectionIntervalMinutes = device.DataCollectionIntervalMinutes,
                Status = device.Status,
                AvailablePlants = await GetPlantsForSelectionAsync(),
                AvailableStatuses = GetDeviceStatusesForSelection()
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
            var existingDevice = await _context.Devices.FindAsync(deviceDto.Id);
            if (existingDevice == null) return Result.Failure("Dispositivo no encontrado para actualizar.");

            existingDevice.Name = deviceDto.Name;
            existingDevice.MacAddress = deviceDto.MacAddress;
            existingDevice.Description = deviceDto.Description;
            existingDevice.PlantId = deviceDto.PlantId > 0 ? deviceDto.PlantId : null;
            existingDevice.DataCollectionIntervalMinutes = deviceDto.DataCollectionIntervalMinutes;
            existingDevice.Status = deviceDto.Status;
            existingDevice.UpdatedAt = DateTime.UtcNow;

            if (existingDevice.PlantId.HasValue)
            {
                var plant = await _context.Plants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == existingDevice.PlantId.Value);
                existingDevice.CropId = plant?.CropId ?? 0;
            }
            else
            {
                existingDevice.CropId = 0;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Dispositivo ID: {DeviceId} actualizado.", deviceDto.Id);
            return Result.Success();
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
            // La base de datos está configurada con ON DELETE CASCADE para las tablas relacionadas.
            // EF Core respetará esta configuración al eliminar el dispositivo.
            var deviceToDelete = await _context.Devices.FindAsync(deviceId);
            if (deviceToDelete == null)
            {
                _logger.LogWarning("Se intentó eliminar un dispositivo inexistente con ID: {DeviceId}", deviceId);
                return Result.Success(); // Es idempotente, si no existe, la operación es "exitosa".
            }

            _context.Devices.Remove(deviceToDelete);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Dispositivo ID: {DeviceId} eliminado exitosamente.", deviceId);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error de base de datos al eliminar dispositivo ID: {DeviceId}. Podría ser por una restricción no configurada para borrado en cascada.", deviceId);
            return Result.Failure($"Error de base de datos al eliminar el dispositivo.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al eliminar dispositivo ID: {DeviceId}", deviceId);
            return Result.Failure($"Error interno: {ex.Message}");
        }
    }

    // --- Métodos para SelectList (Dropdowns) ---

    public async Task<IEnumerable<SelectListItem>> GetPlantsForSelectionAsync()
    {
        try
        {
            return await _context.Plants
                .AsNoTracking()
                .Include(p => p.Crop) // Incluimos el cultivo para acceder a su nombre
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Name} (Cultivo: {p.Crop.Name})"
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lista de plantas para selección.");
            return new List<SelectListItem>();
        }
    }

    public IEnumerable<SelectListItem> GetDeviceStatusesForSelection()
    {
        // No se necesita consulta a la DB, se lee directamente del Enum.
        return Enum.GetValues<DeviceStatus>()
            .Cast<DeviceStatus>()
            .Select(s => new SelectListItem
            {
                Value = s.ToString(),
                Text = s.GetDisplayName()
            })
            .ToList();
    }
}