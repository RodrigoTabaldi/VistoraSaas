using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vistora.Worker;

public sealed class HealthCheckStartupService(
    HealthCheckService healthChecks,
    ILogger<HealthCheckStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var report = await healthChecks.CheckHealthAsync(cancellationToken);
        if (report.Status != HealthStatus.Healthy)
        {
            throw new InvalidOperationException("Worker health check failed during startup.");
        }

        logger.LogInformation("Worker health check passed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
