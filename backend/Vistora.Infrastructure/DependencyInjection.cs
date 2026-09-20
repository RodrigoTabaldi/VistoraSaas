using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using StackExchange.Redis;
using Vistora.Application.Caching;
using Vistora.Application.Messaging;
using Vistora.Application.Persistence;
using Vistora.Application.Storage;
using Vistora.Infrastructure.Caching;
using Vistora.Infrastructure.Messaging;
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
        services.AddScoped<TenantCommandInterceptor>();
        services.AddDbContext<VistoraDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<TenantSessionConnectionInterceptor>(),
                    serviceProvider.GetRequiredService<TenantTransactionInterceptor>(),
                    serviceProvider.GetRequiredService<TenantCommandInterceptor>()));

        services
            .AddOptions<S3StorageOptions>()
            .Bind(configuration.GetSection(S3StorageOptions.SectionName));
        services.AddSingleton<IValidateOptions<S3StorageOptions>, S3StorageOptionsValidator>();
        services.AddSingleton<IAmazonS3>(SupabaseS3ClientFactory.Create);
        services.AddSingleton<IPrivateObjectStorage, SupabaseS3ObjectStorage>();

        var redisConnection = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            throw new InvalidOperationException("ConnectionStrings:Redis must be configured.");
        }

        var rabbitMqConnection = configuration.GetConnectionString("RabbitMq");
        if (string.IsNullOrWhiteSpace(rabbitMqConnection))
        {
            throw new InvalidOperationException("ConnectionStrings:RabbitMq must be configured.");
        }

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<IApplicationCache, RedisApplicationCache>();
        services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis");

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IConnection>(_ =>
            new ConnectionFactory { Uri = new Uri(rabbitMqConnection), AutomaticRecoveryEnabled = true }
                .CreateConnectionAsync().GetAwaiter().GetResult());
        services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq");

        return services;
    }
}
