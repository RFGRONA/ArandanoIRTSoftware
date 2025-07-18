using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ArandanoIRT.Web.Common; // Asegúrate de tener este using si la clase DataQueryFilters está ahí

namespace ArandanoIRT.Web._3_Presentation.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IDataQueryService _dataQueryService;
    private readonly ICropService _cropService;
    private readonly IPlantService _plantService;
    private readonly ILogger<DashboardController> _logger;

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
        _logger.LogInformation("Cargando Dashboard. SelectedCropId: {SelectedCropId}, SelectedPlantId: {SelectedPlantId}", selectedCropId, selectedPlantId);

        var viewModel = new DashboardViewModel
        {
            SelectedCropId = selectedCropId,
            SelectedPlantId = selectedPlantId
        };

        // --- Poblar Filtros (Tu código actual es correcto) ---
        var cropsResult = await _cropService.GetAllCropsAsync();
        if (cropsResult.IsSuccess && cropsResult.Value != null)
        {
            viewModel.AvailableCrops = cropsResult.Value
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name, Selected = c.Id == selectedCropId })
                .OrderBy(s => s.Text).ToList();
        }
        viewModel.AvailableCrops.Insert(0, new SelectListItem("Todos los Cultivos", "") { Selected = !selectedCropId.HasValue });

        if (selectedCropId.HasValue)
        {
            var plantsResult = await _plantService.GetPlantsByCropAsync(selectedCropId.Value);
            if (plantsResult.IsSuccess && plantsResult.Value != null)
            {
                viewModel.AvailablePlants = plantsResult.Value
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name, Selected = p.Id == selectedPlantId })
                    .OrderBy(s => s.Text).ToList();
            }
        }
        viewModel.AvailablePlants.Insert(0, new SelectListItem("Todas las Plantas", "") { Selected = !selectedPlantId.HasValue });
        
        // --- INICIO DE LA CORRECCIÓN ---

        var duration = TimeSpan.FromHours(24);

        // 1. Obtener todos los datos en paralelo (añadimos la llamada para la tabla de capturas)
        var activeDevicesResultTask = _dataQueryService.GetActiveDevicesCountAsync(selectedCropId, selectedPlantId);
        var monitoredPlantsResultTask = _dataQueryService.GetMonitoredPlantsCountAsync(selectedCropId);
        var thermalStatsResultTask = _dataQueryService.GetThermalStatsForDashboardAsync(duration, selectedCropId, selectedPlantId, null);
        var latestAmbientDataResultTask = _dataQueryService.GetLatestAmbientDataAsync(selectedCropId, selectedPlantId, null);
        var ambientDataForChartsResultTask = _dataQueryService.GetAmbientDataForDashboardAsync(duration, selectedCropId, selectedPlantId, null);
        // ===> NUEVA TAREA AÑADIDA
        var recentCapturesTask = _dataQueryService.GetThermalCapturesAsync(new DataQueryFilters { PageSize = 5 });

        await Task.WhenAll(
            activeDevicesResultTask,
            monitoredPlantsResultTask,
            thermalStatsResultTask,
            latestAmbientDataResultTask,
            ambientDataForChartsResultTask,
            recentCapturesTask // ===> Se espera la nueva tarea
        );

        // 2. Poblar el ViewModel con todos los resultados
        var activeDevicesResult = await activeDevicesResultTask;
        if (activeDevicesResult.IsSuccess) viewModel.ActiveDevicesCount = activeDevicesResult.Value;

        var monitoredPlantsResult = await monitoredPlantsResultTask;
        if (monitoredPlantsResult.IsSuccess) viewModel.PlantsMonitoredCount = monitoredPlantsResult.Value;
        
        var thermalStatsResult = await thermalStatsResultTask;
        if (thermalStatsResult.IsSuccess && thermalStatsResult.Value != null)
        {
            viewModel.ThermalStatistics = thermalStatsResult.Value;
        }

        var latestAmbientDataResult = await latestAmbientDataResultTask;
        if (latestAmbientDataResult.IsSuccess && latestAmbientDataResult.Value != null)
        {
            viewModel.LatestAmbientData = latestAmbientDataResult.Value;
        }

        var ambientDataForChartsResult = await ambientDataForChartsResultTask;
        if (ambientDataForChartsResult.IsSuccess && ambientDataForChartsResult.Value != null)
        {
            var data = ambientDataForChartsResult.Value.ToList();
            if (data.Any())
            {
                // Poblar datos para gráficos
                viewModel.TemperatureChartData = new TimeSeriesChartDataDto { DataSetLabel = "Temperatura (°C)", Labels = data.Select(d => d.RecordedAt.ToString("HH:mm")).ToList(), Values = data.Select(d => (float?)d.Temperature).ToList(), BorderColor = "rgb(255, 99, 132)", BackgroundColor = "rgba(255, 99, 132, 0.2)" };
                viewModel.HumidityChartData = new TimeSeriesChartDataDto { DataSetLabel = "Humedad (%)", Labels = data.Select(d => d.RecordedAt.ToString("HH:mm")).ToList(), Values = data.Select(d => (float?)d.Humidity).ToList(), BorderColor = "rgb(54, 162, 235)", BackgroundColor = "rgba(54, 162, 235, 0.2)" };
                viewModel.LightChartData = new TimeSeriesChartDataDto { DataSetLabel = "Luz (lx)", Labels = data.Select(d => d.RecordedAt.ToString("HH:mm")).ToList(), Values = data.Select(d => d.Light).ToList(), BorderColor = "rgb(255, 205, 86)", BackgroundColor = "rgba(255, 205, 86, 0.2)" };

                // Calcular promedios, máximos y mínimos ambientales
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
        else { _logger.LogWarning("No se pudieron cargar los datos para los gráficos: {Error}", ambientDataForChartsResult.ErrorMessage); }

        // ===> SE ASIGNA EL RESULTADO DE LA NUEVA TAREA
        var recentCapturesResult = await recentCapturesTask;
        if(recentCapturesResult.IsSuccess)
        {
            viewModel.RecentCaptures = recentCapturesResult.Value.Items;
        }
        else { _logger.LogWarning("No se pudieron cargar las capturas recientes: {Error}", recentCapturesResult.ErrorMessage); }

        // --- FIN DE LA CORRECCIÓN ---

        return View(viewModel);
    }
}