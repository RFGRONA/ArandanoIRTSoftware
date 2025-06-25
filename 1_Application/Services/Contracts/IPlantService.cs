using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IPlantService
{
    Task<Result<IEnumerable<PlantSummaryDto>>> GetAllPlantsAsync();
    Task<Result<PlantDetailsDto?>> GetPlantByIdAsync(int plantId);
    Task<Result<PlantEditDto?>> GetPlantForEditByIdAsync(int plantId);
    Task<Result<int>> CreatePlantAsync(PlantCreateDto plantDto);
    Task<Result> UpdatePlantAsync(PlantEditDto plantDto);
    Task<Result> DeletePlantAsync(int plantId);
    Task<Result<IEnumerable<PlantSummaryDto>>> GetPlantsByCropAsync(int cropId);

    // Métodos para poblar dropdowns
    Task<IEnumerable<SelectListItem>> GetCropsForSelectionAsync();
    Task<IEnumerable<SelectListItem>> GetStatusesForSelectionAsync();
}