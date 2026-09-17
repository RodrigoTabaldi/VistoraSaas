using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vistora.Application.Storage;
using Vistora.Infrastructure.Storage.S3;

namespace Vistora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddVistoraInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<S3StorageOptions>()
            .Bind(configuration.GetSection(S3StorageOptions.SectionName));
        services.AddSingleton<IValidateOptions<S3StorageOptions>, S3StorageOptionsValidator>();
        services.AddSingleton<IAmazonS3>(SupabaseS3ClientFactory.Create);
        services.AddSingleton<IPrivateObjectStorage, SupabaseS3ObjectStorage>();

        return services;
    }
}
