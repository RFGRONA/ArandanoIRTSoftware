using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.SensorData;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize]
public class DashboardController : Controller
{
    private readonly ICropService _cropService;
    private readonly IDataQueryService _dataQueryService;
    private readonly ILogger<DashboardController> _logger;
    private readonly IPlantService _plantService;

    public DashboardController(
        IDataQueryService dataQueryService,
        ICropService cropService,
        IPlantService plantService,
        ILogger<DashboardController> logger)
    {
        _dataQueryService = dataQueryService;
        _cropService = cropService;
        _plantService = plantService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? selectedCropId = null, int? selectedPlantId = null)
    {
        _logger.LogInformation(
            "Cargando Dashboard. SelectedCropId: {SelectedCropId}, SelectedPlantId: {SelectedPlantId}", selectedCropId,
            selectedPlantId);

        var viewModel = new DashboardViewModel
        {
            SelectedCropId = selectedCropId,
            SelectedPlantId = selectedPlantId
        };

        var cropsResult = await _cropService.GetAllCropsAsync();
        if (cropsResult.IsSuccess)
            viewModel.AvailableCrops = cropsResult.Value
                .Select(c => new SelectListItem
                    { Value = c.Id.ToString(), Text = c.Name, Selected = c.Id == selectedCropId })
                .OrderBy(s => s.Text).ToList();
        viewModel.AvailableCrops.Insert(0,
            new SelectListItem("Todos los Cultivos", "") { Selected = !selectedCropId.HasValue });

        if (selectedCropId.HasValue)
        {
            var plantsResult = await _plantService.GetPlantsByCropAsync(selectedCropId.Value);
            if (plantsResult.IsSuccess)
                viewModel.AvailablePlants = plantsResult.Value
                    .Select(p => new SelectListItem
                        { Value = p.Id.ToString(), Text = p.Name, Selected = p.Id == selectedPlantId })
                    .OrderBy(s => s.Text).ToList();
        }

        viewModel.AvailablePlants.Insert(0,
            new SelectListItem("Todas las Plantas", "") { Selected = !selectedPlantId.HasValue });

        try
        {
            var duration = TimeSpan.FromHours(24);

            var activeDevicesResult =
                await _dataQueryService.GetActiveDevicesCountAsync(selectedCropId, selectedPlantId);
            if (activeDevicesResult.IsSuccess) viewModel.ActiveDevicesCount = activeDevicesResult.Value;

            var monitoredPlantsResult = await _dataQueryService.GetMonitoredPlantsCountAsync(selectedCropId);
            if (monitoredPlantsResult.IsSuccess) viewModel.PlantsMonitoredCount = monitoredPlantsResult.Value;

            var thermalStatsResult =
                await _dataQueryService.GetThermalStatsForDashboardAsync(duration, selectedCropId, selectedPlantId);
            if (thermalStatsResult.IsSuccess)
                viewModel.ThermalStatistics = thermalStatsResult.Value;

            var latestAmbientDataResult =
                await _dataQueryService.GetLatestAmbientDataAsync(selectedCropId, selectedPlantId, null);
            if (latestAmbientDataResult.IsSuccess && latestAmbientDataResult.Value != null)
                viewModel.LatestAmbientData = latestAmbientDataResult.Value;

            var ambientDataForChartsResult =
                await _dataQueryService.GetAmbientDataForDashboardAsync(duration, selectedCropId, selectedPlantId);
            if (ambientDataForChartsResult.IsSuccess)
            {
                var data = ambientDataForChartsResult.Value.ToList();
                if (data.Any())
                {
                    viewModel.TemperatureChartData = new TimeSeriesChartDataDto
                    {
                        DataSetLabel = "Temperatura (°C)",
                        Labels = data.Select(d => d.RecordedAt.ToColombiaTime().ToString("HH:mm")).ToList(),
                        Values = data.Select(d => (float?)d.Temperature).ToList(),
                        BorderColor = "rgb(255, 99, 132)",
                        BackgroundColor = "rgba(255, 99, 132, 0.2)",
                        YAxisOptions = new ChartYAxisOptions { 
                            BeginAtZero = true,
                            Min = 0
                        }
                    };
                    viewModel.HumidityChartData = new TimeSeriesChartDataDto
                    {
                        DataSetLabel = "Humedad (%)",
                        Labels = data.Select(d => d.RecordedAt.ToColombiaTime().ToString("HH:mm")).ToList(),
                        Values = data.Select(d => (float?)d.Humidity).ToList(),
                        BorderColor = "rgb(54, 162, 235)",
                        BackgroundColor = "rgba(54, 162, 235, 0.2)",
                        YAxisOptions = new ChartYAxisOptions { 
                            BeginAtZero = true,
                            Min = 0
                        }
                    };
                    viewModel.LightChartData = new TimeSeriesChartDataDto
                    {
                        DataSetLabel = "Luz (lx)",
                        Labels = data.Select(d => d.RecordedAt.ToColombiaTime().ToString("HH:mm")).ToList(),
                        Values = data.Select(d => d.Light).ToList(),
                        BorderColor = "rgb(255, 205, 86)",
                        BackgroundColor = "rgba(255, 205, 86, 0.2)",
                        YAxisOptions = new ChartYAxisOptions { 
                            BeginAtZero = true,
                            Min = 0
                        }
                    };

                    viewModel.AverageAmbientTemperature24h = data.Average(d => d.Temperature);
                    viewModel.MaxAmbientTemperature24h = data.Max(d => d.Temperature);
                    viewModel.MinAmbientTemperature24h = data.Min(d => d.Temperature);
                    viewModel.AverageAmbientHumidity24h = data.Average(d => d.Humidity);
                    viewModel.MaxAmbientHumidity24h = data.Max(d => d.Humidity);
                    viewModel.MinAmbientHumidity24h = data.Min(d => d.Humidity);
                    var lightValues = data.Where(d => d.Light.HasValue).Select(d => d.Light!.Value).ToList();
                    if (lightValues.Any())
                    {
                        viewModel.AverageAmbientLight24h = lightValues.Average();
                        viewModel.MaxAmbientLight24h = lightValues.Max();
                        viewModel.MinAmbientLight24h = lightValues.Min();
                    }
                }
            }
            else
            {
                _logger.LogWarning("No se pudieron cargar los datos para los gráficos: {Error}",
                    ambientDataForChartsResult.ErrorMessage);
            }

            var recentCapturesResult =
                await _dataQueryService.GetThermalCapturesAsync(new DataQueryFilters { PageSize = 5 });
            if (recentCapturesResult.IsSuccess)
                viewModel.RecentCaptures = recentCapturesResult.Value.Items;
            else
                _logger.LogWarning("No se pudieron cargar las capturas recientes: {Error}",
                    recentCapturesResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Ocurrió una excepción no controlada al cargar los datos del dashboard.");

            ViewData["ErrorMessage"] =
                "No se pudieron cargar los datos del dashboard. Por favor, intente de nuevo más tarde.";
        }

        return View(viewModel);
    }
}