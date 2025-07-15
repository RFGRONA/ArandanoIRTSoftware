using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
public class CropsController : BaseAdminController
{
    private readonly ICropService _cropService;

    public CropsController(ICropService cropService)
    {
        _cropService = cropService;
    }

    // Corrected to use the actual service method
    public async Task<IActionResult> Index()
    {
        var result = await _cropService.GetAllCropsAsync();
        if (!result.IsSuccess)
        {
            // Handle the case where the list cannot be retrieved
            TempData[ErrorMessageKey] = result.ErrorMessage;
            return View(new List<CropSummaryDto>());
        }
        return View(result.Value);
    }

    // Corrected to use the actual service method
    public async Task<IActionResult> Details(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[ErrorMessageKey] = InvalidRequestDataMessage;
            return RedirectToAction(nameof(Index));
        }
        var result = await _cropService.GetCropByIdAsync(id);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage ?? "Crop not found.";
            return RedirectToAction(nameof(Index));
        }
        return View(result.Value);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CropCreateDto cropCreateDto)
    {
        if (!ModelState.IsValid)
        {
            return View(cropCreateDto);
        }
        var result = await _cropService.CreateCropAsync(cropCreateDto);
        return HandleServiceResult(result, nameof(Index), cropCreateDto);
    }

    // Corrected to use the actual service method
    public async Task<IActionResult> Edit(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[ErrorMessageKey] = InvalidRequestDataMessage;
            return RedirectToAction(nameof(Index));
        }
        var result = await _cropService.GetCropForEditByIdAsync(id);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage ?? "Crop not found.";
            return RedirectToAction(nameof(Index));
        }
        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CropEditDto cropEditDto)
    {
        if (id != cropEditDto.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(cropEditDto);
        }

        var result = await _cropService.UpdateCropAsync(cropEditDto);
        return HandleServiceResult(result, nameof(Index), cropEditDto);
    }

    // Show delete confirmation view for a crop
    public async Task<IActionResult> Delete(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[ErrorMessageKey] = InvalidRequestDataMessage;
            return RedirectToAction(nameof(Index));
        }
        var result = await _cropService.GetCropByIdAsync(id);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData[ErrorMessageKey] = result.ErrorMessage ?? "Crop not found.";
            return RedirectToAction(nameof(Index));
        }
        // Optionally, pass a flag or message to indicate this is a delete confirmation
        ViewBag.IsDeleteConfirmation = true;
        return View(result.Value);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[ErrorMessageKey] = InvalidRequestDataMessage;
            return RedirectToAction(nameof(Delete), new { id });
        }

        // The service method name is correct here
        var result = await _cropService.DeleteCropAsync(id);
        return HandleServiceResult(result, nameof(Index), nameof(Delete));
    }
}