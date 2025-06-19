using ArandanoIRT.Web.Common;
using ArandanoIRT.Web.Data.DTOs.Admin;

namespace ArandanoIRT.Web.Services.Contracts;

public interface ICropService
{
    Task<Result<IEnumerable<CropSummaryDto>>> GetAllCropsAsync();
    Task<Result<CropDetailsDto?>> GetCropByIdAsync(int cropId); // Nullable si no se encuentra
    Task<Result<CropEditDto?>> GetCropForEditByIdAsync(int cropId); // Para poblar el DTO de edición
    Task<Result<int>> CreateCropAsync(CropCreateDto cropDto);
    Task<Result> UpdateCropAsync(CropEditDto cropDto);
    Task<Result> DeleteCropAsync(int cropId);
}