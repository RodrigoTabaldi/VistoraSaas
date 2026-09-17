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
}
