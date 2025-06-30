using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web.Common;
using ArandanoIRT.Web._2_Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class CropService : ICropService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CropService> _logger;

    public CropService(ApplicationDbContext context, ILogger<CropService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<int>> CreateCropAsync(CropCreateDto cropDto)
    {
        try
        {
            var newCrop = new Crop
            {
                Name = cropDto.Name,
                Address = cropDto.Address,
                CityName = cropDto.CityName
            };

            _context.Crops.Add(newCrop);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cultivo creado exitosamente con ID: {CropId}", newCrop.Id);
            return Result.Success(newCrop.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al crear el cultivo: {CropName}", cropDto.Name);
            return Result.Failure<int>($"Error interno al crear el cultivo: {ex.Message}");
        }
    }

    public async Task<Result> DeleteCropAsync(int cropId)
    {
        try
        {
            _logger.LogInformation("Intentando eliminar cultivo con ID: {CropId}", cropId);

            var cropToDelete = await _context.Crops.FindAsync(cropId);

            if (cropToDelete == null)
            {
                _logger.LogWarning("No se encontró el cultivo con ID: {CropId} para eliminar. Se considera la operación exitosa.", cropId);
                return Result.Success();
            }

            _context.Crops.Remove(cropToDelete);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cultivo con ID: {CropId} eliminado exitosamente.", cropId);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error de base de datos al eliminar el cultivo con ID: {CropId}. Puede deberse a restricciones de clave externa.", cropId);
            return Result.Failure($"Error de base de datos al eliminar el cultivo. Verifique que no tenga entidades dependientes.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al eliminar el cultivo con ID: {CropId}", cropId);
            return Result.Failure($"Error interno al eliminar el cultivo: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CropSummaryDto>>> GetAllCropsAsync()
    {
        try
        {
            var cropSummaries = await _context.Crops
                .AsNoTracking()
                .Select(c => new CropSummaryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    CityName = c.CityName,
                    CreatedAt = c.CreatedAt.ToLocalTime()
                })
                .ToListAsync();

            return Result.Success<IEnumerable<CropSummaryDto>>(cropSummaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener todos los cultivos.");
            return Result.Failure<IEnumerable<CropSummaryDto>>($"Error interno al obtener cultivos: {ex.Message}");
        }
    }

    public async Task<Result<CropDetailsDto?>> GetCropByIdAsync(int cropId)
    {
        try
        {
            var cropDetails = await _context.Crops
                .AsNoTracking()
                .Where(c => c.Id == cropId)
                .Select(c => new CropDetailsDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    CityName = c.CityName,
                    CreatedAt = c.CreatedAt.ToLocalTime(),
                    UpdatedAt = c.UpdatedAt.ToLocalTime()
                })
                .FirstOrDefaultAsync();

            if (cropDetails == null)
            {
                _logger.LogWarning("Cultivo con ID: {CropId} no encontrado.", cropId);
            }

            return Result.Success<CropDetailsDto?>(cropDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener el cultivo con ID: {CropId}", cropId);
            return Result.Failure<CropDetailsDto?>($"Error interno al obtener el cultivo: {ex.Message}");
        }
    }
    public async Task<Result<CropEditDto?>> GetCropForEditByIdAsync(int cropId)
    {
        try
        {
            var cropEditDto = await _context.Crops
                .AsNoTracking()
                .Where(c => c.Id == cropId)
                .Select(c => new CropEditDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    CityName = c.CityName
                })
                .FirstOrDefaultAsync();

            if (cropEditDto == null)
            {
                _logger.LogWarning("Cultivo con ID: {CropId} no encontrado para edición.", cropId);
            }

            return Result.Success<CropEditDto?>(cropEditDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener el cultivo para edición con ID: {CropId}", cropId);
            return Result.Failure<CropEditDto?>($"Error interno al obtener el cultivo para edición: {ex.Message}");
        }
    }

    public async Task<Result> UpdateCropAsync(CropEditDto cropDto)
    {
        try
        {
            var existingCrop = await _context.Crops.FindAsync(cropDto.Id);

            if (existingCrop == null)
            {
                _logger.LogWarning("No se encontró el cultivo con ID: {CropId} para actualizar.", cropDto.Id);
                return Result.Failure("Cultivo no encontrado para actualizar.");
            }

            existingCrop.Name = cropDto.Name;
            existingCrop.Address = cropDto.Address;
            existingCrop.CityName = cropDto.CityName;
            existingCrop.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cultivo con ID: {CropId} actualizado exitosamente.", cropDto.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al actualizar el cultivo con ID: {CropId}", cropDto.Id);
            return Result.Failure($"Error interno al actualizar el cultivo: {ex.Message}");
        }
    }
}