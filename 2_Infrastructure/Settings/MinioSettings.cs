namespace ArandanoIRT.Web._2_Infrastructure.Settings;

public class MinioSettings
{
    public const string SectionName = "Minio";
    public string Endpoint { get; set; } = null!;
    public string AccessKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public bool UseSsl { get; set; } = false;
    public string PublicUrlBase { get; set; } = string.Empty;
}