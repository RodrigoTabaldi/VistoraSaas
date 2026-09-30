using Microsoft.Extensions.Options;

namespace Vistora.Infrastructure.Storage.S3;

public sealed class S3StorageOptionsValidator : IValidateOptions<S3StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, S3StorageOptions options)
    {
        var failures = new List<string>();

        // HTTP só é permitido em endpoints locais quando a opção de desenvolvimento está habilitada.
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttps &&
             !(options.AllowInsecureLocalEndpoint && endpoint.Scheme == Uri.UriSchemeHttp &&
               endpoint.Host is "rustfs" or "minio" or "localhost" or "127.0.0.1")))
        {
            failures.Add("Storage:S3:Endpoint must be HTTPS, except for an explicitly enabled local S3 endpoint.");
        }

        if (string.IsNullOrWhiteSpace(options.Region)) failures.Add("Storage:S3:Region is required.");
        if (string.IsNullOrWhiteSpace(options.Bucket)) failures.Add("Storage:S3:Bucket is required.");
        if (string.IsNullOrWhiteSpace(options.AccessKeyId)) failures.Add("Storage:S3:AccessKeyId is required.");
        if (string.IsNullOrWhiteSpace(options.SecretAccessKey)) failures.Add("Storage:S3:SecretAccessKey is required.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
