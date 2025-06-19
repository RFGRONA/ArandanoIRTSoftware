using Supabase;
using ArandanoIRT.Web.Common;
using ArandanoIRT.Web.Data.DTOs.Admin;
using ArandanoIRT.Web.Data.Models;
using ArandanoIRT.Web.Services.Contracts;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web.Services.Implementation;

public class PlantService : IPlantService
{
    private readonly Client _supabaseClient;
    private readonly ILogger<PlantService> _logger;

    public PlantService(Client supabaseClient, ILogger<PlantService> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    private Supabase.Interfaces.ISupabaseTable<PlantDataModel, Supabase.Realtime.RealtimeChannel> PlantTable() =>
        _supabaseClient.From<PlantDataModel>();

    private Supabase.Interfaces.ISupabaseTable<CropModel, Supabase.Realtime.RealtimeChannel> CropTable() =>
        _supabaseClient.From<CropModel>();

    private Supabase.Interfaces.ISupabaseTable<StatusModel, Supabase.Realtime.RealtimeChannel> StatusTable() =>
        _supabaseClient.From<StatusModel>();

    public async Task<Result<IEnumerable<PlantSummaryDto>>> GetPlantsByCropAsync(int cropId)
    {
        if (cropId <= 0)
        {
            // Devuelve una lista vacía si el cropId no es válido,
            // o podrías devolver Failure si prefieres manejarlo como error.
            return Result.Success<IEnumerable<PlantSummaryDto>>(new List<PlantSummaryDto>());
        }

        try
        {
            var plantResponse = await PlantTable()
                .Filter("crop_id", Supabase.Postgrest.Constants.Operator.Equals, cropId.ToString())
                .Get();

            if (plantResponse?.Models == null)
                return Result.Success<IEnumerable<PlantSummaryDto>>(new List<PlantSummaryDto>());

            var plants = plantResponse.Models;

            // Optimización: Obtener todos los crops y status necesarios en pocas llamadas
            // Para este método, el cropId ya es conocido, así que solo necesitamos su nombre.
            // Y los status para las plantas filtradas.
            string cropName = "N/A";
            var crop = await CropTable().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, cropId.ToString())
                .Single();
            if (crop != null) cropName = crop.Name;

            var statusIds = plants.Select(p => p.StatusId).Distinct().ToList();
            var statusMap = new Dictionary<int, string>();
            if (statusIds.Any())
            {
                var statusResponse =
                    await StatusTable().Filter("id", Supabase.Postgrest.Constants.Operator.In, statusIds).Get();
                if (statusResponse?.Models != null)
                    statusMap = statusResponse.Models.ToDictionary(s => s.Id, s => s.Name);
            }

            var plantSummaries = plants.Select(p => new PlantSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                CropName = cropName, // Usamos el nombre del cultivo ya obtenido
                StatusName = statusMap.TryGetValue(p.StatusId, out var sn) ? sn : "N/A",
                RegisteredAt = p.RegisteredAt.ToColombiaTime() 
            }).ToList();

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
            var newPlant = new PlantDataModel
            {
                Name = plantDto.Name,
                CropId = plantDto.CropId,
                StatusId = plantDto.StatusId,
                RegisteredAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var response = await PlantTable().Insert(newPlant);
            if (response?.Models != null && response.Models.Any())
            {
                var createdPlant = response.Models.First();
                _logger.LogInformation("Planta creada con ID: {PlantId}", createdPlant.Id);
                return Result.Success(createdPlant.Id);
            }

            _logger.LogError("Error al crear planta. Supabase response: {@response}", response);
            return Result.Failure<int>("No se pudo crear la planta.");
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
            // TODO: Considerar lógica de borrado si hay dispositivos asociados.
            // Nuestra tabla device_data tiene plant_id ON DELETE SET NULL.
            await PlantTable().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, plantId.ToString()).Delete();
            _logger.LogInformation("Solicitud de eliminación para planta ID: {PlantId} enviada.", plantId);
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
            // Para obtener CropName y StatusName, necesitamos hacer "joins" o múltiples consultas.
            // Con supabase-csharp, la forma más directa sin depender de [Reference] complejo es
            // obtener todas las plantas y luego enriquecer la información.
            var plantResponse = await PlantTable().Get();
            if (plantResponse?.Models == null)
                return Result.Success<IEnumerable<PlantSummaryDto>>(new List<PlantSummaryDto>());

            var plants = plantResponse.Models;

            // Optimización: Obtener todos los crops y status necesarios en pocas llamadas
            var cropIds = plants.Where(p => p.CropId.HasValue).Select(p => p.CropId!.Value).Distinct().ToList();
            var statusIds = plants.Select(p => p.StatusId).Distinct().ToList();

            var cropsMap = new Dictionary<int, string>();
            if (cropIds.Any())
            {
                var cropsResponse =
                    await CropTable().Filter("id", Supabase.Postgrest.Constants.Operator.In, cropIds).Get();
                if (cropsResponse?.Models != null)
                    cropsMap = cropsResponse.Models.ToDictionary(cr => cr.Id, cr => cr.Name);
            }

            var statusMap = new Dictionary<int, string>();
            if (statusIds.Any())
            {
                var statusResponse =
                    await StatusTable().Filter("id", Supabase.Postgrest.Constants.Operator.In, statusIds).Get();
                if (statusResponse?.Models != null)
                    statusMap = statusResponse.Models.ToDictionary(s => s.Id, s => s.Name);
            }

            var plantSummaries = plants.Select(p => new PlantSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                CropName = p.CropId.HasValue && cropsMap.TryGetValue(p.CropId.Value, out var cn) ? cn : "N/A",
                StatusName = statusMap.TryGetValue(p.StatusId, out var sn) ? sn : "N/A",
                RegisteredAt = p.RegisteredAt.ToLocalTime()
            }).ToList();

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
            var plant = await PlantTable()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, plantId.ToString())
                .Single();

            if (plant == null) return Result.Success<PlantDetailsDto?>(null);

            string cropName = "N/A";
            string cropCity = "N/A";
            if (plant.CropId.HasValue)
            {
                var crop = await CropTable().Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                    plant.CropId.Value.ToString()).Single();
                if (crop != null)
                {
                    cropName = crop.Name;
                    cropCity = crop.CityName;
                }
            }

            var status = await StatusTable()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, plant.StatusId.ToString()).Single();

            var details = new PlantDetailsDto
            {
                Id = plant.Id,
                Name = plant.Name,
                CropName = cropName,
                CropCityName = cropCity,
                StatusName = status?.Name ?? "N/A",
                RegisteredAt = plant.RegisteredAt.ToLocalTime(),
                UpdatedAt = plant.UpdatedAt.ToLocalTime()
            };
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
            var plant = await PlantTable()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, plantId.ToString())
                .Single();

            if (plant == null) return Result.Success<PlantEditDto?>(null);

            var editDto = new PlantEditDto
            {
                Id = plant.Id,
                Name = plant.Name,
                CropId = plant.CropId ?? 0, // Asignar 0 si es null para que el dropdown no falle
                StatusId = plant.StatusId,
                AvailableCrops = await GetCropsForSelectionAsync(),
                AvailableStatuses = await GetStatusesForSelectionAsync()
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
            var existingPlant = await PlantTable()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, plantDto.Id.ToString())
                .Single();
            if (existingPlant == null) return Result.Failure("Planta no encontrada para actualizar.");

            existingPlant.Name = plantDto.Name;
            existingPlant.CropId = plantDto.CropId;
            existingPlant.StatusId = plantDto.StatusId;
            // UpdatedAt se actualiza por trigger

            var response = await PlantTable().Update(existingPlant);
            if (response?.ResponseMessage?.IsSuccessStatusCode == true)
            {
                _logger.LogInformation("Planta ID: {PlantId} actualizada.", plantDto.Id);
                return Result.Success();
            }

            _logger.LogError("Error al actualizar planta ID {PlantId}. Supabase response: {@response}", plantDto.Id,
                response);
            return Result.Failure("No se pudo actualizar la planta.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al actualizar planta ID: {PlantId}", plantDto.Id);
            return Result.Failure($"Error interno: {ex.Message}");
        }
    }

    // Métodos para Dropdowns
    public async Task<IEnumerable<SelectListItem>> GetCropsForSelectionAsync()
    {
        try
        {
            var response = await CropTable().Get(); // Obtener todos los cultivos
            if (response?.Models != null)
            {
                return response.Models.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lista de cultivos para selección.");
        }

        return new List<SelectListItem>();
    }

    public async Task<IEnumerable<SelectListItem>> GetStatusesForSelectionAsync()
    {
        try
        {
            // Podrías filtrar los status relevantes para Plantas si tuvieras muchos
            // Por ahora, obtenemos todos.
            var response = await StatusTable().Order("name", Supabase.Postgrest.Constants.Ordering.Ascending).Get();
            if (response?.Models != null)
            {
                return response.Models.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lista de estados para selección.");
        }

        return new List<SelectListItem>();
    }
}