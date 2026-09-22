using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vistora.Application.Messaging;
using Vistora.Application.Persistence;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Infrastructure.Reporting;

public sealed class ReportJobRetryScanner(
    IServiceScopeFactory serviceScopeFactory,
    IReportJobPublisher publisher,
    ILogger<ReportJobRetryScanner> logger) : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var totalReenqueued = 0;

                List<Guid> organizationIds;
                await using (var initScope = serviceScopeFactory.CreateAsyncScope())
                {
                    var initDbContext = initScope.ServiceProvider.GetRequiredService<IVistoraDbContext>();
                    organizationIds = await initDbContext.Organizations
                        .Select(o => o.Id)
                        .ToListAsync(stoppingToken);
                }

                foreach (var organizationId in organizationIds)
                {
                    try
                    {
                        await using var orgScope = serviceScopeFactory.CreateAsyncScope();

                        var tenantContext = orgScope.ServiceProvider.GetRequiredService<TenantContext>();
                        tenantContext.SetOrganization(organizationId);

                        var orgDbContext = orgScope.ServiceProvider.GetRequiredService<IVistoraDbContext>();

                        var pendingRetryJobs = await orgDbContext.ReportJobs
                            .Where(j => j.Status == ReportJobStatus.Pending && j.Attempts > 0)
                            .ToListAsync(stoppingToken);

                        foreach (var job in pendingRetryJobs)
                        {
                            await publisher.PublishAsync(new ReportJobMessage(job.Id, organizationId), stoppingToken);
                            totalReenqueued++;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error scanning retry report jobs for organization '{OrganizationId}'", organizationId);
                    }
                }

                if (totalReenqueued > 0)
                {
                    logger.LogInformation("ReportJobRetryScanner re-enqueued {Count} pending retry job(s) across organizations.", totalReenqueued);
                }
                else
                {
                    logger.LogDebug("ReportJobRetryScanner completed scan with 0 pending retry jobs to re-enqueue.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error during ReportJobRetryScanner execution cycle.");
            }

            try
            {
                await Task.Delay(ScanInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
