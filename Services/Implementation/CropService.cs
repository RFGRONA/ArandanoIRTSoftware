using Microsoft.Extensions.Options;
using Supabase; // Supabase.Client
using ArandanoIRT.Web.Configuration; // SupabaseSettings
// CropModel
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArandanoIRT.Web.Common;
using ArandanoIRT.Web.Data.DTOs.Admin;
using ArandanoIRT.Web.Data.Models;
using ArandanoIRT.Web.Services.Contracts;

namespace ArandanoIRT.Web.Services.Implementation;

public class CropService : ICropService
{
    private readonly Client _supabaseClient;
    private readonly ILogger<CropService> _logger;

    public CropService(Client supabaseClient, ILogger<CropService> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    private Supabase.Interfaces.ISupabaseTable<CropModel, Supabase.Realtime.RealtimeChannel> CropTable() =>
        _supabaseClient.From<CropModel>();

    public async Task<Result<int>> CreateCropAsync(CropCreateDto cropDto)
    {
        try
        {
            var newCrop = new CropModel
            {
                Name = cropDto.Name,
                Address = cropDto.Address,
                CityName = cropDto.CityName,
                CreatedAt = DateTime.UtcNow, // El trigger de DB también lo hace, pero es bueno ser explícito
                UpdatedAt = DateTime.UtcNow  // El trigger de DB también lo hace
            };

            var response = await CropTable().Insert(newCrop);

            if (response?.Models != null && response.Models.Any())
            {
                var createdCrop = response.Models.First();
                _logger.LogInformation("Cultivo creado exitosamente con ID: {CropId}", createdCrop.Id);
                return Result.Success(createdCrop.Id);
            }
            else
            {
                _logger.LogError("Error al crear el cultivo. Respuesta de Supabase nula o vacía. Mensaje: {ResponseMessage}", response?.ResponseMessage?.ToString());
                return Result.Failure<int>("No se pudo crear el cultivo en la base de datos.");
            }
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
            // Considerar verificar si hay entidades dependientes (Plantas) antes de eliminar.
            // Por ahora, eliminación directa. La FK en Supabase (ON DELETE SET NULL / RESTRICT) manejará la integridad.
            // Nuestra tabla plant_data tiene crop_id ON DELETE SET NULL, lo cual está bien.

            await CropTable().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, cropId.ToString()).Delete();
            // Delete no devuelve un cuerpo de respuesta significativo que indique éxito en el borrado de filas directamente,
            // pero si no lanza excepción, asumimos que funcionó o que no había nada que borrar.
            // Podríamos hacer un Get previo para asegurar que existe si es necesario.
            _logger.LogInformation("Solicitud de eliminación para cultivo ID: {CropId} enviada.", cropId);
            return Result.Success();
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
            var response = await CropTable().Get();
            if (response?.Models != null)
            {
                var cropSummaries = response.Models.Select(c => new CropSummaryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    CityName = c.CityName,
                    CreatedAt = c.CreatedAt.ToLocalTime() // Convertir a local si se almacenó como UTC
                }).ToList();
                return Result.Success<IEnumerable<CropSummaryDto>>(cropSummaries);
            }
            _logger.LogWarning("No se encontraron cultivos o hubo un error al recuperarlos.");
            return Result.Success<IEnumerable<CropSummaryDto>>(new List<CropSummaryDto>()); // Devolver lista vacía en lugar de fallo
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
            var response = await CropTable()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, cropId.ToString())
                .Single();

            if (response != null)
            {
                var cropDetails = new CropDetailsDto
                {
                    Id = response.Id,
                    Name = response.Name,
                    Address = response.Address,
                    CityName = response.CityName,
                    CreatedAt = response.CreatedAt.ToLocalTime(),
                    UpdatedAt = response.UpdatedAt.ToLocalTime()
                };
                return Result.Success<CropDetailsDto?>(cropDetails);
            }
            _logger.LogWarning("Cultivo con ID: {CropId} no encontrado.", cropId);
            return Result.Success<CropDetailsDto?>(null); // No es un fallo, simplemente no encontrado
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
            var response = await CropTable()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, cropId.ToString())
                .Single();

            if (response != null)
            {
                var cropEditDto = new CropEditDto
                {
                    Id = response.Id,
                    Name = response.Name,
                    Address = response.Address,
                    CityName = response.CityName
                };
                return Result.Success<CropEditDto?>(cropEditDto);
            }
            _logger.LogWarning("Cultivo con ID: {CropId} no encontrado para edición.", cropId);
            return Result.Success<CropEditDto?>(null);
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
            // Obtener el registro existente para no perder CreatedAt y otras propiedades no editables directamente
            var existingCrop = await CropTable()
                                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, cropDto.Id.ToString())
                                    .Single();

            if (existingCrop == null)
            {
                _logger.LogWarning("No se encontró el cultivo con ID: {CropId} para actualizar.", cropDto.Id);
                return Result.Failure("Cultivo no encontrado para actualizar.");
            }

            // Actualizar solo los campos modificables
            existingCrop.Name = cropDto.Name;
            existingCrop.Address = cropDto.Address;
            existingCrop.CityName = cropDto.CityName;
            // existingCrop.UpdatedAt se actualizará por el trigger en la DB

            var response = await CropTable().Update(existingCrop);

            if (response?.ResponseMessage?.IsSuccessStatusCode == true)
            {
                _logger.LogInformation("Cultivo con ID: {CropId} actualizado exitosamente.", cropDto.Id);
                return Result.Success();
            }
            else
            {
                _logger.LogError("Error al actualizar el cultivo con ID: {CropId}. Mensaje de Supabase: {ResponseMessage}", cropDto.Id, response?.ResponseMessage?.ToString());
                return Result.Failure("No se pudo actualizar el cultivo en la base de datos.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al actualizar el cultivo con ID: {CropId}", cropDto.Id);
            return Result.Failure($"Error interno al actualizar el cultivo: {ex.Message}");
        }
    }
}