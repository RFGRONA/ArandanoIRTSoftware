using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.DTOs.Crops;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface ICropService
{
    Task<Result<IEnumerable<CropSummaryDto>>> GetAllCropsAsync();
    Task<Result<CropDetailsDto?>> GetCropByIdAsync(int cropId); // Nullable si no se encuentra
    Task<Result<CropEditDto?>> GetCropForEditByIdAsync(int cropId); // Para poblar el DTO de edición
    Task<Result<int>> CreateCropAsync(CropCreateDto cropDto);
    Task<Result> UpdateCropAsync(CropEditDto cropDto);
    Task<Result> DeleteCropAsync(int cropId);
}