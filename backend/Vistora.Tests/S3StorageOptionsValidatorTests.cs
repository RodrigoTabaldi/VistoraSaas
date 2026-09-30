using Microsoft.Extensions.Options;
using Vistora.Infrastructure.Storage.S3;
using Xunit;

namespace Vistora.Tests;

public sealed class S3StorageOptionsValidatorTests
{
    [Fact]
    public void Validate_accepts_complete_supabase_s3_configuration()
    {
        var options = new S3StorageOptions
        {
            Endpoint = "https://project.storage.supabase.co/storage/v1/s3",
            Region = "sa-east-1",
            Bucket = "vistora-private",
            AccessKeyId = "access-key",
            SecretAccessKey = "secret-key"
        };

        var result = new S3StorageOptionsValidator().Validate(Options.DefaultName, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_rejects_incomplete_or_insecure_configuration()
    {
        var result = new S3StorageOptionsValidator().Validate(Options.DefaultName, new S3StorageOptions
        {
            Endpoint = "http://localhost/storage/v1/s3"
        });

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("http://minio:9000")]
    [InlineData("http://rustfs:9000")]
    public void Validate_accepts_only_explicit_local_http_storage(string endpoint)
    {
        var options = new S3StorageOptions
        {
            Endpoint = endpoint, Region = "us-east-1", Bucket = "vistora-private",
            AccessKeyId = "local-user", SecretAccessKey = "local-secret",
            AllowInsecureLocalEndpoint = true
        };
        var validator = new S3StorageOptionsValidator();
        Assert.True(validator.Validate(Options.DefaultName, options).Succeeded);
        Assert.True(validator.Validate(Options.DefaultName, new S3StorageOptions
        {
            Endpoint = "http://remote.example:9000", Region = options.Region, Bucket = options.Bucket,
            AccessKeyId = options.AccessKeyId, SecretAccessKey = options.SecretAccessKey,
            AllowInsecureLocalEndpoint = true
        }).Failed);
    }
}
