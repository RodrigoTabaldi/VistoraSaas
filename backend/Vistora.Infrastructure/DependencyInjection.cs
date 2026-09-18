using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vistora.Application.Persistence;
using Vistora.Application.Storage;
using Vistora.Infrastructure.Persistence.PostgreSql;
using Vistora.Infrastructure.Storage.S3;

namespace Vistora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddVistoraInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Postgres must be configured.");
        }

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(serviceProvider => serviceProvider.GetRequiredService<TenantContext>());
        services.AddScoped<TenantSessionConnectionInterceptor>();
        services.AddScoped<TenantTransactionInterceptor>();
        services.AddDbContext<VistoraDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<TenantSessionConnectionInterceptor>(),
                    serviceProvider.GetRequiredService<TenantTransactionInterceptor>()));

        services
            .AddOptions<S3StorageOptions>()
            .Bind(configuration.GetSection(S3StorageOptions.SectionName));
        services.AddSingleton<IValidateOptions<S3StorageOptions>, S3StorageOptionsValidator>();
        services.AddSingleton<IAmazonS3>(SupabaseS3ClientFactory.Create);
        services.AddSingleton<IPrivateObjectStorage, SupabaseS3ObjectStorage>();

        return services;
    }
}
