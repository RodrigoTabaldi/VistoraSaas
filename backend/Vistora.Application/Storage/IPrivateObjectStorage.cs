namespace Vistora.Application.Storage;

/// <summary>Abstraction for private object storage used by application use cases.</summary>
public interface IPrivateObjectStorage
{
    Task UploadAsync(StorageUpload upload, CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    Uri CreateDownloadUrl(string objectKey, TimeSpan lifetime);
}

public sealed record StorageUpload(
    string ObjectKey,
    Stream Content,
    string ContentType,
    long? ContentLength = null);
