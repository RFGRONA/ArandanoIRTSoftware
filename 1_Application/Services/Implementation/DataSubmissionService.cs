using System.Text.Json;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.DeviceApi;
using ArandanoIRT.Web._1_Application.DTOs.Weather;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data; // Usando el DbContext
using Microsoft.EntityFrameworkCore; // Usando EF Core

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class DataSubmissionService : IDataSubmissionService
{
    private readonly ApplicationDbContext _context;
    private readonly IWeatherService _weatherService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<DataSubmissionService> _logger;

    // El nombre del bucket ahora puede venir de configuración o ser una constante.
    private const string RgbImageBucketName = "rgb-captures";

    public DataSubmissionService(
        ApplicationDbContext context,
        IWeatherService weatherService,
        IFileStorageService fileStorageService,
        ILogger<DataSubmissionService> logger)
    {
        _context = context;
        _weatherService = weatherService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<Result> SaveAmbientDataAsync(DeviceIdentityContext deviceContext, AmbientDataDto ambientDataDto)
    {
        _logger.LogInformation("Guardando datos ambientales para DeviceId: {DeviceId}", deviceContext.DeviceId);

        WeatherInfo? weatherInfo = null;
        if (deviceContext.CropId > 0)
        {
            // Buscamos el cultivo directamente con EF Core.
            var crop = await _context.Crops
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == deviceContext.CropId);

            if (crop != null && !string.IsNullOrWhiteSpace(crop.CityName))
            {
                var weatherResult = await _weatherService.GetCurrentWeatherAsync(crop.CityName);
                if (weatherResult.IsSuccess)
                {
                    weatherInfo = weatherResult.Value;
                    _logger.LogInformation("WeatherAPI para {City}: Temp={Temp}, Hum={Hum}, IsNight={IsNight}",
                        crop.CityName, weatherInfo.TemperatureCelsius, weatherInfo.HumidityPercentage, weatherInfo.IsNight);
                }
                else
                {
                    _logger.LogWarning("No se pudo obtener el clima para {City}: {Error}", crop.CityName, weatherResult.ErrorMessage);
                }
            }
            else
            {
                _logger.LogWarning("No se pudo obtener el cultivo (CropId: {CropId}) o el nombre de la ciudad está vacío.", deviceContext.CropId);
            }
        }

        var extraData = new Dictionary<string, object>();

        if (ambientDataDto.Light.HasValue)
        {
            extraData["light"] = ambientDataDto.Light.Value;
        }
        if (ambientDataDto.Pressure.HasValue)
        {
            extraData["pressure"] = ambientDataDto.Pressure.Value;
        }
        if (weatherInfo?.IsNight.HasValue == true)
        {
            extraData["is_night"] = weatherInfo.IsNight.Value;
        }

        var sensorDataRecord = new EnvironmentalReading
        {
            DeviceId = deviceContext.DeviceId,
            PlantId = deviceContext.PlantId,
            Temperature = ambientDataDto.Temperature,
            Humidity = ambientDataDto.Humidity,
            CityTemperature = weatherInfo?.TemperatureCelsius,
            CityHumidity = weatherInfo?.HumidityPercentage,
            CityWeatherCondition = weatherInfo?.ConditionText,
            ExtraData = extraData.Any() ? JsonSerializer.Serialize(extraData) : null,
            RecordedAtServer = DateTime.UtcNow,
            RecordedAtDevice = ambientDataDto.RecordedAtDevice?.ToSafeUniversalTime()
        };

        try
        {
            _context.EnvironmentalReadings.Add(sensorDataRecord);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Datos ambientales guardados exitosamente para DeviceId {DeviceId}. Nuevo ID: {NewId}",
                deviceContext.DeviceId, sensorDataRecord.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al guardar datos ambientales para DeviceId {DeviceId}.", deviceContext.DeviceId);
            return Result.Failure($"Error interno del servidor al guardar datos ambientales: {ex.Message}");
        }
    }

    public async Task<Result> SaveCaptureDataAsync(
        DeviceIdentityContext deviceContext,
        ThermalDataDto thermalDataDto,
        string thermalDataJsonString,
        IFormFile imageFile,
        DateTime recordedAtServer)
    {
        _logger.LogInformation("Guardando datos de captura para DeviceId: {DeviceId}", deviceContext.DeviceId);

        bool? isNight = null;
        if (deviceContext.CropId > 0)
        {
            var crop = await _context.Crops.AsNoTracking().FirstOrDefaultAsync(c => c.Id == deviceContext.CropId);
            if (crop != null && !string.IsNullOrWhiteSpace(crop.CityName))
            {
                var weatherResult = await _weatherService.GetCurrentWeatherAsync(crop.CityName);
                if (weatherResult.IsSuccess)
                {
                    isNight = weatherResult.Value.IsNight;
                }
            }
        }

        string? uploadedImagePath = null;
        // Solo subir si hay imagen y no es de noche
        if (imageFile != null && imageFile.Length > 0 && isNight == false)
        {
            _logger.LogInformation("Es de día, procediendo a subir imagen RGB para DeviceId: {DeviceId}", deviceContext.DeviceId);
            var fileNameInBucket = $"{deviceContext.DeviceId}_{recordedAtServer:yyyyMMddHHmmssfff}{Path.GetExtension(imageFile.FileName)}";

            // Usamos el nuevo servicio de almacenamiento
            var uploadResult = await _fileStorageService.UploadFileAsync(imageFile, RgbImageBucketName, fileNameInBucket);

            if (uploadResult.IsSuccess)
            {
                uploadedImagePath = uploadResult.Value;
                _logger.LogInformation("Imagen subida exitosamente. URL: {ImageUrl}", uploadedImagePath);
            }
            else
            {
                _logger.LogError("Error al subir imagen a través de IFileStorageService: {Error}", uploadResult.ErrorMessage);
            }
        }
        else if (isNight == true)
        {
            _logger.LogInformation("Es de noche. No se subirá la imagen RGB para DeviceId: {DeviceId}.", deviceContext.DeviceId);
        }

        var thermalDataRecord = new ThermalCapture
        {
            DeviceId = deviceContext.DeviceId,
            PlantId = deviceContext.PlantId,
            ThermalDataStats = thermalDataJsonString,
            RgbImagePath = uploadedImagePath,
            RecordedAtServer = recordedAtServer,
            RecordedAtDevice = thermalDataDto.RecordedAtDevice?.ToSafeUniversalTime()
        };

        try
        {
            _context.ThermalCaptures.Add(thermalDataRecord);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Datos de captura guardados exitosamente para DeviceId {DeviceId}. Nuevo ID: {NewId}.",
                deviceContext.DeviceId, thermalDataRecord.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al guardar datos de captura para DeviceId {DeviceId}.", deviceContext.DeviceId);
            return Result.Failure($"Error interno del servidor al guardar datos de captura: {ex.Message}");
        }
    }
}