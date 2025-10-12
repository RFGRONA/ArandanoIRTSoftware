using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace ArandanoIRT.Web._2_Infrastructure.Services;

/// <summary>
/// Implementación del servicio de almacenamiento de archivos que utiliza MinIO, un servidor de almacenamiento de objetos compatible con S3.
/// </summary>
public class MinioStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;
    private readonly ILogger<MinioStorageService> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="MinioStorageService"/>.
    /// Configura y construye el cliente de MinIO con las credenciales y el endpoint proporcionados.
    /// </summary>
    public MinioStorageService(IOptions<MinioSettings> settingsOptions, ILogger<MinioStorageService> logger)
    {
        _settings = settingsOptions.Value;
        _logger = logger;

        _logger.LogInformation("Servicio de MinIO configurado. Endpoint: {MinioEndpoint}", _settings.Endpoint);

        _minioClient = new MinioClient()
            .WithEndpoint(_settings.Endpoint)
            .WithCredentials(_settings.AccessKey, _settings.SecretKey)
            .WithSSL(_settings.UseSsl)
            .Build();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Esta implementación primero verifica si el contenedor (bucket) de destino existe.
    /// Si no existe, lo crea y le aplica una política de acceso de solo lectura pública para que los archivos puedan ser accedidos a través de una URL.
    /// Luego, sube el archivo al bucket y construye la URL pública correspondiente.
    /// </remarks>
    public async Task<Result<string>> UploadFileAsync(IFormFile file, string containerName, string fileName)
    {
        _logger.LogInformation("Iniciando subida de archivo {FileName} al bucket {BucketName}.", fileName, containerName);
        try
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(containerName);
            _logger.LogDebug("Verificando existencia del bucket {BucketName}...", containerName);
            bool found = await _minioClient.BucketExistsAsync(bucketExistsArgs);

            if (!found)
            {
                _logger.LogInformation("El bucket {BucketName} no existe. Se procederá a crearlo.", containerName);
                var makeBucketArgs = new MakeBucketArgs().WithBucket(containerName);
                await _minioClient.MakeBucketAsync(makeBucketArgs);

                _logger.LogInformation("Configurando política de acceso público para el bucket {BucketName}...", containerName);
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
                _logger.LogInformation("Bucket {BucketName} creado y configurado exitosamente.", containerName);
            }

            await using var stream = file.OpenReadStream();
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(containerName)
                .WithObject(fileName)
                .WithStreamData(stream)
                .WithObjectSize(file.Length)
                .WithContentType(file.ContentType);

            await _minioClient.PutObjectAsync(putObjectArgs);

            string publicUrl = $"{_settings.PublicUrlBase.TrimEnd('/')}/{containerName}/{fileName}";

            _logger.LogInformation("Archivo {FileName} ({FileSizeInBytes} bytes) subido exitosamente al bucket {BucketName}.", fileName, file.Length, containerName);

            return Result.Success(publicUrl);
        }
        catch (MinioException minEx)
        {
            _logger.LogError(minEx, "Error de MinIO al intentar subir el archivo {FileName} al bucket {BucketName}", fileName, containerName);
            return Result.Failure<string>($"Error de almacenamiento (MinIO): {minEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción inesperada durante la subida del archivo {FileName}", fileName);
            return Result.Failure<string>($"Error interno del servidor al subir el archivo: {ex.Message}");
        }
    }
}