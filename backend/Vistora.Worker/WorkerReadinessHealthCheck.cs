using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vistora.Worker;

public sealed class WorkerReadinessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy("Worker is ready."));
}
