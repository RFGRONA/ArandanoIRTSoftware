using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Plants;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class PlantService : IPlantService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlantService> _logger;

    public PlantService(ApplicationDbContext context, ILogger<PlantService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<PlantSummaryDto>>> GetPlantsByCropAsync(int cropId)
    {
        if (cropId <= 0) return Result.Success<IEnumerable<PlantSummaryDto>>(new List<PlantSummaryDto>());

        try
        {
            // Consulta única y eficiente que trae la planta y el nombre del cultivo asociado.
            var plantSummaries = await _context.Plants
                .AsNoTracking()
                .Where(p => p.CropId == cropId)
                .Select(p => new PlantSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    CropName = p.Crop.Name, // Navegación directa gracias a EF Core
                    // StatusName ya no existe en la entidad Plant.
                    RegisteredAt = p.RegisteredAt
                })
                .ToListAsync();

            return Result.Success<IEnumerable<PlantSummaryDto>>(plantSummaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener plantas para el cultivo ID: {CropId}", cropId);
            return Result.Failure<IEnumerable<PlantSummaryDto>>(
                $"Error interno al obtener plantas por cultivo: {ex.Message}");
        }
    }

    public async Task<Result<int>> CreatePlantAsync(PlantCreateDto plantDto)
    {
        try
        {
            var newPlant = new Plant
            {
                Name = plantDto.Name,
                CropId = plantDto.CropId,
                Status = plantDto.Status ?? PlantStatus.UNKNOWN,
                ExperimentalGroup = plantDto.ExperimentalGroup ?? ExperimentalGroupType.MONITORED,
                RegisteredAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Plants.Add(newPlant);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Planta creada con ID: {PlantId}", newPlant.Id);
            return Result.Success(newPlant.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al crear planta: {PlantName}", plantDto.Name);
            return Result.Failure<int>($"Error interno: {ex.Message}");
        }
    }

    public async Task<Result> DeletePlantAsync(int plantId)
    {
        try
        {
            var plantToDelete = await _context.Plants.FindAsync(plantId);
            if (plantToDelete == null)
            {
                _logger.LogWarning("Se intentó eliminar una planta inexistente con ID: {PlantId}", plantId);
                return Result.Success();
            }

            // La configuración ON DELETE SET NULL en la DB se encargará de los dispositivos asociados.
            _context.Plants.Remove(plantToDelete);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Planta ID: {PlantId} eliminada.", plantId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al eliminar planta ID: {PlantId}", plantId);
            return Result.Failure($"Error interno: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<PlantSummaryDto>>> GetAllPlantsAsync()
    {
        try
        {
            var plantSummaries = await _context.Plants
                .AsNoTracking()
                .Include(p => p.Crop) // Incluimos el cultivo para acceder a su nombre
                .Select(p => new PlantSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    CropName = p.Crop.Name,
                    // StatusName ya no existe.
                    RegisteredAt = p.RegisteredAt
                })
                .ToListAsync();

            return Result.Success<IEnumerable<PlantSummaryDto>>(plantSummaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener todas las plantas.");
            return Result.Failure<IEnumerable<PlantSummaryDto>>($"Error interno: {ex.Message}");
        }
    }

    public async Task<Result<PlantDetailsDto?>> GetPlantByIdAsync(int plantId)
    {
        try
        {
            var details = await _context.Plants
                .AsNoTracking()
                .Where(p => p.Id == plantId)
                .Select(p => new PlantDetailsDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    CropName = p.Crop.Name,
                    CropCityName = p.Crop.CityName,
                    StatusName = p.Status.ToString(),
                    ExperimentalGroup = p.ExperimentalGroup.ToString(),
                    RegisteredAt = p.RegisteredAt,
                    UpdatedAt = p.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (details == null) _logger.LogWarning("Planta con ID {PlantId} no encontrada.", plantId);

            return Result.Success(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener planta ID: {PlantId}", plantId);
            return Result.Failure<PlantDetailsDto?>($"Error interno: {ex.Message}");
        }
    }

    public async Task<Result<PlantEditDto?>> GetPlantForEditByIdAsync(int plantId)
    {
        try
        {
            var plant = await _context.Plants.FindAsync(plantId);
            if (plant == null) return Result.Success<PlantEditDto?>(null);

            var editDto = new PlantEditDto
            {
                Id = plant.Id,
                Name = plant.Name,
                CropId = plant.CropId,
                Status = plant.Status,
                ExperimentalGroup = plant.ExperimentalGroup,
                AvailableCrops = await GetCropsForSelectionAsync(),
                AvailableExperimentalGroups = GetExperimentalGroupsForSelection()
            };
            return Result.Success<PlantEditDto?>(editDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener planta para editar ID: {PlantId}", plantId);
            return Result.Failure<PlantEditDto?>($"Error interno: {ex.Message}");
        }
    }

    public async Task<Result> UpdatePlantAsync(PlantEditDto plantDto)
    {
        try
        {
            var existingPlant = await _context.Plants.FindAsync(plantDto.Id);
            if (existingPlant == null) return Result.Failure("Planta no encontrada para actualizar.");

            existingPlant.Name = plantDto.Name;
            existingPlant.CropId = plantDto.CropId;
            existingPlant.Status = plantDto.Status.Value;
            existingPlant.ExperimentalGroup = plantDto.ExperimentalGroup.Value;
            existingPlant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Planta ID: {PlantId} actualizada.", plantDto.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al actualizar planta ID: {PlantId}", plantDto.Id);
            return Result.Failure($"Error interno: {ex.Message}");
        }
    }

    public async Task<IEnumerable<SelectListItem>> GetPlantsForSelectionAsync()
    {
        try
        {
            return await _context.Plants
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                }).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lista de plantas para selección.");
            // Devuelve una lista vacía en caso de error para no romper la vista
            return new List<SelectListItem>();
        }
    }

    public async Task<Result> UpdatePlantStatusAsync(int plantId, PlantStatus newStatus, string? observation,
        int userId)
    {
        // Usamos una transacción para asegurar que ambas operaciones (actualizar planta y crear historial)
        // se completen exitosamente o ninguna lo haga.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var plant = await _context.Plants.FindAsync(plantId);
            if (plant == null) return Result.Failure("Planta no encontrada.");

            // 1. Actualizar el estado actual en la tabla de plantas
            plant.Status = newStatus;
            plant.UpdatedAt = DateTime.UtcNow;

            // 2. Crear un nuevo registro en la tabla de historial
            var historyRecord = new PlantStatusHistory
            {
                PlantId = plantId,
                Status = newStatus,
                Observation = observation,
                UserId = userId, // Guardamos el ID del usuario que hizo el cambio
                ChangedAt = DateTime.UtcNow
            };
            _context.PlantStatusHistories.Add(historyRecord);

            // 3. Guardar todos los cambios en la base de datos
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Estado de la planta ID {PlantId} actualizado a {NewStatus} por el usuario ID {UserId}", plantId,
                newStatus, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error al actualizar el estado de la planta ID {PlantId}", plantId);
            return Result.Failure($"Error interno al actualizar el estado: {ex.Message}");
        }
    }

    public async Task<IEnumerable<PlantStatusHistoryDto>> GetPlantStatusHistoryAsync(int? plantId, int? userId,
        DateTime? startDate, DateTime? endDate)
    {
        var query = _context.PlantStatusHistories.AsNoTracking();

        // Aplicar filtros dinámicamente
        if (plantId.HasValue) query = query.Where(h => h.PlantId == plantId.Value);
        if (userId.HasValue) query = query.Where(h => h.UserId == userId.Value);
        if (startDate.HasValue) query = query.Where(h => h.ChangedAt.Date >= startDate.Value.Date);
        if (endDate.HasValue) query = query.Where(h => h.ChangedAt.Date <= endDate.Value.Date);

        // Proyectar el resultado al DTO y ordenar por fecha
        var historyList = await query
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new PlantStatusHistoryDto
            {
                Id = h.Id,
                PlantName = h.Plant.Name,
                Status = h.Status.ToString(),
                Observation = h.Observation,
                // Si UserId es nulo, fue el sistema. Si no, mostramos el nombre del usuario.
                Source = h.User == null ? "Sistema" : h.User.FirstName + " " + h.User.LastName,
                ChangedAt = h.ChangedAt
            })
            .ToListAsync();

        return historyList;
    }


    // --- Métodos para Dropdowns ---

    public async Task<IEnumerable<SelectListItem>> GetCropsForSelectionAsync()
    {
        try
        {
            return await _context.Crops
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lista de cultivos para selección.");
            return new List<SelectListItem>();
        }
    }

    public IEnumerable<SelectListItem> GetExperimentalGroupsForSelection()
    {
        return Enum.GetValues(typeof(ExperimentalGroupType))
            .Cast<ExperimentalGroupType>()
            .Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = e.ToString()
            }).ToList();
    }
}