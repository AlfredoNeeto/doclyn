namespace Doclyn.Application.Common.Options;

/// <summary>
/// Configurações de armazenamento de objetos lidas de appsettings.json → seção "Storage".
/// </summary>
public sealed class StorageOptions
{
    public const string Section = "Storage";

    public string Provider { get; init; } = "Minio";
    public string Endpoint { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public bool UseSsl { get; init; } = false;
    public string BucketName { get; init; } = "doclyn-documents";
}
