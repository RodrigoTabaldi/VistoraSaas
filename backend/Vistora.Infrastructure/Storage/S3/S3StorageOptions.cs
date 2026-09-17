namespace Vistora.Infrastructure.Storage.S3;

public sealed class S3StorageOptions
{
    public const string SectionName = "Storage:S3";

    public string Endpoint { get; init; } = string.Empty;

    public string Region { get; init; } = string.Empty;

    public string Bucket { get; init; } = string.Empty;

    public string AccessKeyId { get; init; } = string.Empty;

    public string SecretAccessKey { get; init; } = string.Empty;
}
