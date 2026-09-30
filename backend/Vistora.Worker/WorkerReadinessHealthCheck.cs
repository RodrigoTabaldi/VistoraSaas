using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Vistora.Worker;

public sealed class WorkerReadinessHealthCheck(
    IConnection rabbitMqConnection,
    IConnectionMultiplexer redisConnection) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) => Task.FromResult(
        Evaluate(rabbitMqConnection.IsOpen, redisConnection.IsConnected));

    // A disponibilidade depende de RabbitMQ e Redis, usados no processamento.
    public static HealthCheckResult Evaluate(bool rabbitMqAvailable, bool redisAvailable) =>
        rabbitMqAvailable && redisAvailable
            ? HealthCheckResult.Healthy("Worker dependencies are connected.")
            : HealthCheckResult.Unhealthy("Worker requires RabbitMQ and Redis connections.");
}
