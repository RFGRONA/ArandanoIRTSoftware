using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web.Common;
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
        if (cropId <= 0)
        {
            return Result.Success<IEnumerable<PlantSummaryDto>>(new List<PlantSummaryDto>());
        }

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
            return Result.Failure<IEnumerable<PlantSummaryDto>>($"Error interno al obtener plantas por cultivo: {ex.Message}");
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
                // StatusId ya no existe.
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
                    // StatusName ya no existe.
                    RegisteredAt = p.RegisteredAt,
                    UpdatedAt = p.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (details == null)
            {
                _logger.LogWarning("Planta con ID {PlantId} no encontrada.", plantId);
            }

            return Result.Success<PlantDetailsDto?>(details);
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
                // StatusId ya no existe.
                AvailableCrops = await GetCropsForSelectionAsync(),
                AvailableStatuses = new List<SelectListItem>() // Devolvemos lista vacía ya que no hay estados.
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
            // StatusId ya no existe.
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

    public Task<IEnumerable<SelectListItem>> GetStatusesForSelectionAsync()
    {
        _logger.LogInformation("GetStatusesForSelectionAsync para Plantas fue llamado, pero las plantas ya no tienen estados. Devolviendo lista vacía.");
        // Las plantas ya no tienen un estado en el nuevo esquema.
        return Task.FromResult<IEnumerable<SelectListItem>>(new List<SelectListItem>());
    }
}