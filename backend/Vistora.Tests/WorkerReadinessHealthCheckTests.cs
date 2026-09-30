using Microsoft.Extensions.Diagnostics.HealthChecks;
using Vistora.Worker;
using Xunit;

namespace Vistora.Tests;

public sealed class WorkerReadinessHealthCheckTests
{
    [Theory]
    [InlineData(true, true, HealthStatus.Healthy)]
    [InlineData(false, true, HealthStatus.Unhealthy)]
    [InlineData(true, false, HealthStatus.Unhealthy)]
    [InlineData(false, false, HealthStatus.Unhealthy)]
    public void Evaluate_reports_health_from_required_dependencies(
        bool rabbitMqAvailable,
        bool redisAvailable,
        HealthStatus expectedStatus)
    {
        var result = WorkerReadinessHealthCheck.Evaluate(rabbitMqAvailable, redisAvailable);

        Assert.Equal(expectedStatus, result.Status);
    }
}
