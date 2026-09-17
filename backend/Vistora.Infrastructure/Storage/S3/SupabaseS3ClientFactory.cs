using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Vistora.Infrastructure.Storage.S3;

public static class SupabaseS3ClientFactory
{
    public static IAmazonS3 Create(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<S3StorageOptions>>().Value;
        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
        var configuration = new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            AuthenticationRegion = options.Region,
            ForcePathStyle = true
        };

        return new AmazonS3Client(credentials, configuration);
    }
}
