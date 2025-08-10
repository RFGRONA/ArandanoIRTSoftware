using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace ArandanoIRT.Web._2_Infrastructure.Services;

public class MinioStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(IOptions<MinioSettings> settingsOptions, ILogger<MinioStorageService> logger)
    {
        _settings = settingsOptions.Value;
        _logger = logger;

        _minioClient = new MinioClient()
            .WithEndpoint(_settings.Endpoint)
            .WithCredentials(_settings.AccessKey, _settings.SecretKey)
            .WithSSL(_settings.UseSsl)
            .Build();
    }

    public async Task<Result<string>> UploadFileAsync(IFormFile file, string containerName, string fileName)
    {
        try
        {
            // 1. Verificar si el bucket (contenedor) existe.
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(containerName);
            bool found = await _minioClient.BucketExistsAsync(bucketExistsArgs);
            if (!found)
            {
                _logger.LogInformation("El bucket '{BucketName}' no existe. Creándolo...", containerName);
                var makeBucketArgs = new MakeBucketArgs().WithBucket(containerName);
                await _minioClient.MakeBucketAsync(makeBucketArgs);

                // 2. Hacer el bucket público para que las URLs funcionen sin autenticación.
                // Esta política permite la lectura pública de todos los objetos en el bucket.
                string policyJson = $@"{{
                    ""Version"": ""2012-10-17"",
                    ""Statement"": [
                        {{
                            ""Effect"": ""Allow"",
                            ""Principal"": {{ ""AWS"": [""*""] }},
                            ""Action"": [""s3:GetObject""],
                            ""Resource"": [""arn:aws:s3:::{containerName}/*""]
                        }}
                    ]
                }}";
                var setPolicyArgs = new SetPolicyArgs().WithBucket(containerName).WithPolicy(policyJson);
                await _minioClient.SetPolicyAsync(setPolicyArgs);
                _logger.LogInformation("Bucket '{BucketName}' creado y configurado como público.", containerName);
            }

            // 3. Subir el archivo al bucket.
            await using var stream = file.OpenReadStream();
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(containerName)
                .WithObject(fileName)
                .WithStreamData(stream)
                .WithObjectSize(file.Length)
                .WithContentType(file.ContentType);

            await _minioClient.PutObjectAsync(putObjectArgs);
            _logger.LogInformation("Archivo '{FileName}' subido exitosamente al bucket '{BucketName}'.", fileName, containerName);

            // 4. Construir y devolver la URL pública del archivo.
            string publicUrl = $"{(_settings.UseSsl ? "https" : "http")}://{_settings.Endpoint}/{containerName}/{fileName}";

            return Result.Success(publicUrl);
        }
        catch (MinioException minEx)
        {
            _logger.LogError(minEx, "Error de MinIO al subir el archivo '{FileName}' al bucket '{BucketName}'.", fileName, containerName);
            return Result.Failure<string>($"Error de almacenamiento (MinIO): {minEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción inesperada al subir el archivo '{FileName}'.", fileName);
            return Result.Failure<string>($"Error interno del servidor al subir el archivo: {ex.Message}");
        }
    }
}