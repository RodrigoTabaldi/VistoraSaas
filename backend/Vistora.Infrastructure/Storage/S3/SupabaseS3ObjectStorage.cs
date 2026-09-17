using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Vistora.Application.Storage;

namespace Vistora.Infrastructure.Storage.S3;

public sealed class SupabaseS3ObjectStorage(
    IAmazonS3 client,
    IOptions<S3StorageOptions> options) : IPrivateObjectStorage
{
    private readonly S3StorageOptions _options = options.Value;

    public async Task UploadAsync(StorageUpload upload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        ValidateObjectKey(upload.ObjectKey);
        ArgumentNullException.ThrowIfNull(upload.Content);

        var request = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = upload.ObjectKey,
            InputStream = upload.Content,
            ContentType = upload.ContentType,
            AutoCloseStream = false
        };

        if (upload.ContentLength is not null)
        {
            request.Headers.ContentLength = upload.ContentLength.Value;
        }

        await client.PutObjectAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        ValidateObjectKey(objectKey);
        await client.DeleteObjectAsync(_options.Bucket, objectKey, cancellationToken);
    }

    public Uri CreateDownloadUrl(string objectKey, TimeSpan lifetime)
    {
        ValidateObjectKey(objectKey);
        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime), "The signed URL lifetime must be positive.");
        }

        var url = client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime)
        });

        return new Uri(url, UriKind.Absolute);
    }

    private static void ValidateObjectKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey) || objectKey.StartsWith('/') || objectKey.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("The object key must be a relative storage key without path traversal.", nameof(objectKey));
        }
    }
}
