using Microsoft.Extensions.Diagnostics.HealthChecks;
using Vistora.Worker;
using Xunit;

namespace Vistora.Tests;

public sealed class WorkerReadinessHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_returns_healthy()
    {
        var check = new WorkerReadinessHealthCheck();

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }
}
