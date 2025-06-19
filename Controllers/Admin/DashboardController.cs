using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ArandanoIRT.Web.Data.DTOs.Admin;
using ArandanoIRT.Web.Services.Contracts;

namespace ArandanoIRT.Web.Controllers.Admin;

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

    public async Task<IActionResult>
        Index(int? selectedCropId = null, int? selectedPlantId = null) // Cambiado nombre de parámetros
    {
        _logger.LogInformation(
            "Cargando Dashboard. SelectedCropId: {SelectedCropId}, SelectedPlantId: {SelectedPlantId}", selectedCropId,
            selectedPlantId);

        var viewModel = new DashboardViewModel
        {
            SelectedCropId = selectedCropId,
            SelectedPlantId = selectedPlantId,
            // Las listas se inicializan vacías en el ViewModel
        };

        // Poblar filtro de Cultivos
        var cropsResult = await _cropService.GetAllCropsAsync();
        if (cropsResult.IsSuccess && cropsResult.Value != null)
        {
            viewModel.AvailableCrops = cropsResult.Value
                .Select(c => new SelectListItem
                    { Value = c.Id.ToString(), Text = c.Name, Selected = c.Id == selectedCropId })
                .OrderBy(s => s.Text)
                .ToList(); // Convertir a List<SelectListItem>
        }

        viewModel.AvailableCrops.Insert(0,
            new SelectListItem("Todos los Cultivos", "") { Selected = !selectedCropId.HasValue });

        // Poblar filtro de Plantas si hay un cultivo seleccionado
        if (selectedCropId.HasValue)
        {
            var plantsResult = await _plantService.GetPlantsByCropAsync(selectedCropId.Value); // Usar el nuevo método
            if (plantsResult.IsSuccess && plantsResult.Value != null)
            {
                viewModel.AvailablePlants = plantsResult.Value
                    .Select(p => new SelectListItem
                        { Value = p.Id.ToString(), Text = p.Name, Selected = p.Id == selectedPlantId })
                    .OrderBy(s => s.Text)
                    .ToList(); // Convertir a List<SelectListItem>
            }
        }

        // Siempre añadir la opción "Todas las Plantas..." incluso si la lista está vacía, para que el dropdown no desaparezca si no hay plantas.
        // El controlador de la vista puede decidir ocultar el dropdown si Model.AvailablePlants tiene solo el item "Todas..."
        viewModel.AvailablePlants.Insert(0,
            new SelectListItem("Todas las Plantas (del cultivo)", "") { Selected = !selectedPlantId.HasValue });


        var duration = TimeSpan.FromHours(24);

        // --- Obtener todos los datos necesarios ---
        var activeDevicesResultTask = _dataQueryService.GetActiveDevicesCountAsync(selectedCropId, selectedPlantId);
        var monitoredPlantsResultTask = _dataQueryService.GetMonitoredPlantsCountAsync(selectedCropId);
        var thermalStatsResultTask =
            _dataQueryService.GetThermalStatsForDashboardAsync(duration, selectedCropId, selectedPlantId, null);
        var latestAmbientDataResultTask =
            _dataQueryService.GetLatestAmbientDataAsync(selectedCropId, selectedPlantId, null);
        var ambientDataForChartsResultTask =
            _dataQueryService.GetAmbientDataForDashboardAsync(duration, selectedCropId, selectedPlantId, null);

        await Task.WhenAll(
            activeDevicesResultTask,
            monitoredPlantsResultTask,
            thermalStatsResultTask,
            latestAmbientDataResultTask,
            ambientDataForChartsResultTask
        );

        // --- Poblar ViewModel ---
        var activeDevicesResult = await activeDevicesResultTask;
        if (activeDevicesResult.IsSuccess) viewModel.ActiveDevicesCount = activeDevicesResult.Value;

        var monitoredPlantsResult = await monitoredPlantsResultTask;
        if (monitoredPlantsResult.IsSuccess) viewModel.PlantsMonitoredCount = monitoredPlantsResult.Value;

        var thermalStatsResult = await thermalStatsResultTask;
        if (thermalStatsResult.IsSuccess && thermalStatsResult.Value != null)
        {
            viewModel.ThermalStatistics = thermalStatsResult.Value;
            _logger.LogDebug(
                "[DashboardData Populate] ThermalStatistics.LatestThermalReadingTimestamp: {TimestampValue}",
                viewModel.ThermalStatistics.LatestThermalReadingTimestamp?.ToString("o"));
        }
        else
        {
            _logger.LogWarning("No se pudieron cargar las estadísticas térmicas: {Error}",
                thermalStatsResult.ErrorMessage);
            viewModel.ThermalStatistics = new ThermalStatsDto();
        }

        var latestAmbientDataResult = await latestAmbientDataResultTask;
        if (latestAmbientDataResult.IsSuccess && latestAmbientDataResult.Value != null)
        {
            viewModel.LatestAmbientData = latestAmbientDataResult.Value;
            _logger.LogDebug("[DashboardData Populate] LatestAmbientData.RecordedAt: {TimestampValue}",
                viewModel.LatestAmbientData.RecordedAt.ToString("o"));
        }
        else
        {
            _logger.LogWarning("No se pudieron cargar los últimos datos ambientales: {Error}",
                latestAmbientDataResult.ErrorMessage);
        }

        var ambientDataForChartsResult = await ambientDataForChartsResultTask;
        if (ambientDataForChartsResult.IsSuccess && ambientDataForChartsResult.Value != null)
        {
            var data = ambientDataForChartsResult.Value.ToList();
            if (data.Any())
            {
                // Poblar datos para gráficos (como antes)
                viewModel.TemperatureChartData = new TimeSeriesChartDataDto
                {
                    DataSetLabel = "Temperatura Amb. (°C)",
                    Labels = data.Select(d => d.RecordedAt.ToString("HH:mm")).ToList(),
                    Values = data.Select(d => (float?)d.Temperature).ToList(),
                    BorderColor = "rgb(255, 99, 132)", BackgroundColor = "rgba(255, 99, 132, 0.2)", PointRadius = 2
                };
                viewModel.HumidityChartData = new TimeSeriesChartDataDto
                {
                    DataSetLabel = "Humedad Amb. (%)",
                    Labels = data.Select(d => d.RecordedAt.ToString("HH:mm")).ToList(),
                    Values = data.Select(d => (float?)d.Humidity).ToList(),
                    BorderColor = "rgb(54, 162, 235)", BackgroundColor = "rgba(54, 162, 235, 0.2)", PointRadius = 2
                };
                viewModel.LightChartData = new TimeSeriesChartDataDto
                {
                    DataSetLabel = "Luz Amb. (lx)",
                    Labels = data.Select(d => d.RecordedAt.ToString("HH:mm")).ToList(),
                    Values = data.Select(d => d.Light).ToList(),
                    BorderColor = "rgb(255, 205, 86)", BackgroundColor = "rgba(255, 205, 86, 0.2)", PointRadius = 2
                };

                // Calcular promedios, máximos y mínimos ambientales
                viewModel.AverageAmbientTemperature24h = data.Average(d => d.Temperature);
                viewModel.MaxAmbientTemperature24h = data.Max(d => d.Temperature); // NUEVO
                viewModel.MinAmbientTemperature24h = data.Min(d => d.Temperature); // NUEVO

                viewModel.AverageAmbientHumidity24h = data.Average(d => d.Humidity);
                viewModel.MaxAmbientHumidity24h = data.Max(d => d.Humidity); // NUEVO
                viewModel.MinAmbientHumidity24h = data.Min(d => d.Humidity); // NUEVO

                var lightValues = data.Where(d => d.Light.HasValue).Select(d => d.Light.Value).ToList();
                if (lightValues.Any())
                {
                    viewModel.AverageAmbientLight24h = lightValues.Average();
                    viewModel.MaxAmbientLight24h = lightValues.Max(); // NUEVO
                    viewModel.MinAmbientLight24h = lightValues.Min(); // NUEVO
                }
            }
        }
        else
        {
            _logger.LogWarning(
                "No se pudieron cargar los datos ambientales para los gráficos y promedios del dashboard: {Error}",
                ambientDataForChartsResult.ErrorMessage);
        }

        return View(viewModel);
    }
}