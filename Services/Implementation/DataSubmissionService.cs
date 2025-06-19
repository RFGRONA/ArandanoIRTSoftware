using Microsoft.Extensions.Options;
using Supabase; // Supabase.Client y Supabase.Storage.Client
using System.Text.Json; // Para JsonSerializer
using System;
using System.IO;
using System.Threading.Tasks;
using ArandanoIRT.Web.Common;
using ArandanoIRT.Web.Configuration;
using ArandanoIRT.Web.Data.DTOs.DeviceApi;
using ArandanoIRT.Web.Data.Models;
using ArandanoIRT.Web.Services.Contracts;
using Microsoft.AspNetCore.Http;
using Supabase.Postgrest.Models;

namespace ArandanoIRT.Web.Services.Implementation;

public class DataSubmissionService : IDataSubmissionService
{
    private readonly Client _supabaseClient; // Supabase.Client
    private readonly SupabaseSettings _supabaseSettings;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<DataSubmissionService> _logger;
    private readonly IHttpClientFactory _httpClientFactory; // Para WeatherAPI

    // Nombre del bucket de Supabase Storage
    private const string RgbImageBucketName = "rgb-captures"; // El nombre que definimos

    public DataSubmissionService(
        Client supabaseClient,
        IOptions<SupabaseSettings> supabaseSettingsOptions,
        IWeatherService weatherService,
        ILogger<DataSubmissionService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _supabaseClient = supabaseClient;
        _supabaseSettings = supabaseSettingsOptions.Value;
        _weatherService = weatherService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private Supabase.Interfaces.ISupabaseTable<T, Supabase.Realtime.RealtimeChannel> GetTable<T>() where T : BaseModel, new()
    {
        return _supabaseClient.From<T>();
    }

    public async Task<Result> SaveAmbientDataAsync(DeviceIdentityContext deviceContext, AmbientDataDto ambientDataDto)
{
    _logger.LogInformation("Guardando datos ambientales para DeviceId: {DeviceId}", deviceContext.DeviceId);

    float? cityTemp = null;
    int? cityHum = null; // WeatherInfo.HumidityPercentage es int?
    bool? isNight = null;
    string? cityCondition = null; 

    if (deviceContext.CropId > 0)
    {
        var cropResult = await GetCropAsync(deviceContext.CropId);
        if (cropResult.IsSuccess && cropResult.Value != null && !string.IsNullOrWhiteSpace(cropResult.Value.CityName))
        {
            // La llamada a GetCurrentWeatherAsync ya está configurada para obtener ConditionText en español
            var weatherResult = await _weatherService.GetCurrentWeatherAsync(cropResult.Value.CityName);
            if (weatherResult.IsSuccess && weatherResult.Value != null)
            {
                cityTemp = weatherResult.Value.TemperatureCelsius;
                cityHum = weatherResult.Value.HumidityPercentage;
                isNight = weatherResult.Value.IsNight;
                cityCondition = weatherResult.Value.ConditionText; // OBTENER ConditionText
                _logger.LogInformation("WeatherAPI para {City}: Temp={CityTemp}, Hum={CityHum}, IsNight={IsNight}, Condición='{CityCondition}'",
                    cropResult.Value.CityName, cityTemp, cityHum, isNight, cityCondition);
            }
            else
            {
                _logger.LogWarning("No se pudo obtener el clima para {City}: {Error}", cropResult.Value.CityName, weatherResult.ErrorMessage);
            }
        }
        else
        {
             _logger.LogWarning("No se pudo obtener la información del cultivo (CropId: {CropId}) o el nombre de la ciudad está vacío. Error si GetCropAsync falló: {Error}", 
                deviceContext.CropId, cropResult.IsFailure ? cropResult.ErrorMessage : "CityName vacío o crop nulo");
        }
    }
    else
    {
         _logger.LogInformation("No se proporcionó CropId para DeviceId: {DeviceId}. No se consultará WeatherAPI.", deviceContext.DeviceId);
    }

    var sensorDataRecord = new SensorDataModel
    {
        DeviceId = deviceContext.DeviceId,
        PlantId = deviceContext.PlantId,
        CropId = deviceContext.CropId,
        Light = ambientDataDto.Light,
        Temperature = ambientDataDto.Temperature,
        Humidity = ambientDataDto.Humidity,
        CityTemperature = cityTemp,
        CityHumidity = cityHum, // Model.CityHumidity debe ser float? para ser compatible con DTO.SensorData.CityHumidity (float?)
                                // WeatherInfo.HumidityPercentage es int?, así que si CityHumidity es float?, la asignación es implícita.
        IsNight = isNight,
        CityWeatherCondition = cityCondition, // NUEVO: Guardar la condición del clima
        RecordedAt = DateTime.UtcNow
    };

    try
    {
        var insertResult = await _supabaseClient.From<SensorDataModel>().Insert(sensorDataRecord); // Usar _supabaseClient.From directamente
        if (insertResult?.Models == null || !insertResult.Models.Any())
        {
            _logger.LogError("Error al insertar datos ambientales para DeviceId {DeviceId}. Response: {Response}",
                deviceContext.DeviceId, insertResult?.ResponseMessage?.ReasonPhrase);
            return Result.Failure("Error al guardar datos ambientales en la base de datos.");
        }
        _logger.LogInformation("Datos ambientales guardados exitosamente para DeviceId {DeviceId}. Nuevo ID: {NewId}",
            deviceContext.DeviceId, insertResult.Models.First().Id);
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
            var cropResult = await GetCropAsync(deviceContext.CropId);
            if (cropResult.IsSuccess && !string.IsNullOrWhiteSpace(cropResult.Value?.CityName)) // Añadido Value?
            {
                var weatherResult = await _weatherService.GetCurrentWeatherAsync(cropResult.Value.CityName);
                if (weatherResult.IsSuccess)
                {
                    isNight = weatherResult.Value.IsNight;
                    _logger.LogInformation("WeatherAPI para captura (Device {DeviceId}, City {City}): IsNight={IsNight}",
                        deviceContext.DeviceId, cropResult.Value.CityName, isNight);
                }
                else
                {
                    _logger.LogWarning("No se pudo obtener el clima para captura (Device {DeviceId}, City {City}): {Error}",
                         deviceContext.DeviceId, cropResult.Value.CityName, weatherResult.ErrorMessage);
                }
            }
            else
            {
                 _logger.LogWarning("No se pudo obtener la información del cultivo para captura (CropId: {CropId}, Device {DeviceId}) o el nombre de la ciudad está vacío. Error: {Error}",
                    deviceContext.CropId, deviceContext.DeviceId, cropResult.ErrorMessage);
            }
        }
        else
        {
             _logger.LogInformation("No se proporcionó CropId para captura (Device {DeviceId}). No se determinará día/noche por WeatherAPI.", deviceContext.DeviceId);
        }

        string? uploadedImagePath = null;
        // Solo intentar subir imagen si es de día o no se pudo determinar (isNight == null)
        if (isNight == false || isNight == null) 
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                _logger.LogInformation("Es de día (o no se pudo determinar), procediendo a subir imagen RGB para DeviceId: {DeviceId}", deviceContext.DeviceId);
                try
                {
                    var storageBucket = _supabaseClient.Storage.From(RgbImageBucketName);
                    // Nombre del archivo dentro del bucket (sin prefijos de bucket)
                    var fileNameInBucket = $"{deviceContext.DeviceId}_{recordedAtServer:yyyyMMddHHmmssfff}_{Guid.NewGuid().ToString("N")}{Path.GetExtension(imageFile.FileName)}";
                    var tempFilePath = Path.GetTempFileName();

                    await using (var stream = new FileStream(tempFilePath, FileMode.Create)) {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Subir el archivo usando fileNameInBucket como el path dentro del bucket
                    var storageResponseKey = await storageBucket.Upload(tempFilePath, fileNameInBucket, new Supabase.Storage.FileOptions { CacheControl = "3600", Upsert = false });
                    File.Delete(tempFilePath);

                    if (!string.IsNullOrEmpty(storageResponseKey)) 
                    {
                        // storageResponseKey es la clave/path del objeto tal como se almacenó.
                        // Para GetPublicUrl, generalmente se espera el path relativo al bucket.
                        // Usamos fileNameInBucket porque es el path relativo que controlamos.
                        string publicUrlFromSDK = _supabaseClient.Storage.From(RgbImageBucketName).GetPublicUrl(fileNameInBucket);
                        
                        _logger.LogInformation("Imagen {FileNameInBucket} subida. Key devuelta: '{StorageResponseKey}'. URL SDK: '{PublicUrlFromSDK}'", 
                            fileNameInBucket, storageResponseKey, publicUrlFromSDK);

                        // Forzar la doble barra si es necesario y si la URL del SDK no la tiene.
                        // Esto es específico para tu caso donde la URL con "//" funciona.
                        // Patrón base: https://<project_ref>.supabase.co/storage/v1/object/public/<bucket_name>
                        // Lo que queremos: <base_url>//<file_name_in_bucket>

                        // Obtener la URL base del bucket de forma segura
                        // GetPublicUrl("") devuelve la URL base del bucket, usualmente terminando en "/"
                        string bucketBaseUrl = _supabaseClient.Storage.From(RgbImageBucketName).GetPublicUrl(""); 
                        
                        if (bucketBaseUrl.EndsWith("/"))
                        {
                            // bucketBaseUrl ya termina en "/", así que solo necesitamos añadir otra "/" y el nombre del archivo
                            uploadedImagePath = $"{bucketBaseUrl}{fileNameInBucket.TrimStart('/')}";
                        }
                        else
                        {
                            // Si bucketBaseUrl no termina en "/", añadimos "//"
                            uploadedImagePath = $"{bucketBaseUrl}//{fileNameInBucket.TrimStart('/')}";
                        }
                        
                        _logger.LogInformation("URL final ajustada para {FileNameInBucket} (con intento de //): {AdjustedImagePath}", fileNameInBucket, uploadedImagePath);

                    } else {
                        _logger.LogError("Error al subir imagen a Supabase Storage para DeviceId {DeviceId}. La respuesta del Upload (clave) fue nula o vacía.", deviceContext.DeviceId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Excepción al subir imagen RGB para DeviceId {DeviceId}.", deviceContext.DeviceId);
                }
            }
            else
            {
                _logger.LogInformation("No se proporcionó archivo de imagen o el archivo está vacío para DeviceId: {DeviceId}", deviceContext.DeviceId);
            }
        }
        else
        {
            _logger.LogInformation("Es de noche (isNight={IsNightValue}) para DeviceId: {DeviceId}. No se subirá la imagen RGB.", isNight, deviceContext.DeviceId);
        }

        var thermalDataRecord = new ThermalDataModel
        {
            DeviceId = deviceContext.DeviceId,
            PlantId = deviceContext.PlantId,
            CropId = deviceContext.CropId,
            ThermalImageData = thermalDataJsonString,
            RgbImagePath = uploadedImagePath, // Será null si la subida falló o era de noche
            RecordedAt = recordedAtServer
        };

        try
        {
            var insertResult = await _supabaseClient.From<ThermalDataModel>().Insert(thermalDataRecord); // Usar _supabaseClient.From directamente
            if (insertResult?.Models == null || !insertResult.Models.Any())
            {
                _logger.LogError("Error al insertar datos de captura (térmicos) para DeviceId {DeviceId}. Response: {Response}",
                    deviceContext.DeviceId, insertResult?.ResponseMessage?.ReasonPhrase);
                return Result.Failure("Error al guardar datos de captura en la base de datos.");
            }
            _logger.LogInformation("Datos de captura guardados exitosamente para DeviceId {DeviceId}. Nuevo ID: {NewId}. Imagen RGB guardada: {ImagePathProvided}",
                deviceContext.DeviceId, insertResult.Models.First().Id, !string.IsNullOrEmpty(uploadedImagePath));
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al guardar datos de captura para DeviceId {DeviceId}.", deviceContext.DeviceId);
            return Result.Failure($"Error interno del servidor al guardar datos de captura: {ex.Message}");
        }
    }

     public async Task<Result> SaveDeviceLogAsync(DeviceIdentityContext deviceContext, DeviceLogEntryDto logEntryDto)
    {
        _logger.LogInformation("Guardando log de dispositivo para DeviceId: {DeviceId}, Tipo: {LogType}, TempInterna: {TempInt}, HumInterna: {HumInt}",
            deviceContext.DeviceId, logEntryDto.LogType, logEntryDto.InternalDeviceTemperature, logEntryDto.InternalDeviceHumidity);

        var deviceLogRecord = new DeviceLogModel
        {
            DeviceId = deviceContext.DeviceId,
            LogType = logEntryDto.LogType,
            LogMessage = logEntryDto.LogMessage,
            LogTimestampServer = DateTime.UtcNow, // Confirmado: usar hora del servidor
            
            // Manejar NaN: si es NaN, guardar null. Si es null en el DTO, se guarda null.
            InternalDeviceTemperature = (logEntryDto.InternalDeviceTemperature.HasValue && float.IsNaN(logEntryDto.InternalDeviceTemperature.Value))
                                        ? null
                                        : logEntryDto.InternalDeviceTemperature,
            InternalDeviceHumidity = (logEntryDto.InternalDeviceHumidity.HasValue && float.IsNaN(logEntryDto.InternalDeviceHumidity.Value))
                                        ? null
                                        : logEntryDto.InternalDeviceHumidity
        };

        try
        {
            var insertResult = await GetTable<DeviceLogModel>().Insert(deviceLogRecord);
            if (insertResult?.Models == null || !insertResult.Models.Any())
            {
                _logger.LogError("Error al insertar log de dispositivo para DeviceId {DeviceId}. Response: {Response}",
                    deviceContext.DeviceId, insertResult?.ResponseMessage?.ReasonPhrase);
                return Result.Failure("Error al guardar log de dispositivo en la base de datos.");
            }
            _logger.LogDebug("Log de dispositivo guardado exitosamente para DeviceId {DeviceId}. Nuevo ID: {NewId}",
                 deviceContext.DeviceId, insertResult.Models.First().Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al guardar log de dispositivo para DeviceId {DeviceId}.", deviceContext.DeviceId);
            return Result.Failure($"Error interno del servidor al guardar log de dispositivo: {ex.Message}");
        }
    }
    
    private async Task<Result<CropModel>> GetCropAsync(int cropId)
    {
        if (cropId <= 0) return Result.Failure<CropModel>("CropId inválido para buscar datos del cultivo.");
        try
        {
            var crop = await _supabaseClient.From<CropModel>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, cropId.ToString())
                .Single();
            if (crop == null) return Result.Failure<CropModel>($"Cultivo con ID {cropId} no encontrado.");
            return Result.Success(crop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener CropModel para CropId {CropId}", cropId);
            return Result.Failure<CropModel>("Error obteniendo datos del cultivo.");
        }
    }
}