using Microsoft.EntityFrameworkCore;
using Vistora.Application.Messaging;
using Vistora.Application.Persistence;
using Vistora.Domain;

namespace Vistora.Application.UseCases;

public sealed class CompleteInspectionUseCase(
    IVistoraDbContext dbContext,
    IReportJobPublisher publisher,
    ITenantContext tenantContext)
{
    public async Task<CompleteInspectionResult> ExecuteAsync(
        Guid inspectionId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var inspection = await dbContext.Inspections
            .FirstOrDefaultAsync(x => x.Id == inspectionId, cancellationToken);

        if (inspection is null)
        {
            return new CompleteInspectionResult.NotFound();
        }

        if (inspection.Status is InspectionStatus.Completed or InspectionStatus.Approved)
        {
            return new CompleteInspectionResult.InvalidState();
        }

        var existingJob = await dbContext.ReportJobs
            .FirstOrDefaultAsync(
                x => x.InspectionId == inspectionId &&
                     (x.Status == ReportJobStatus.Pending || x.Status == ReportJobStatus.Processing),
                cancellationToken);

        if (existingJob is not null)
        {
            return new CompleteInspectionResult.AlreadyInProgress(existingJob.Id);
        }

        var now = DateTimeOffset.UtcNow;
        inspection.Status = InspectionStatus.Completed;
        inspection.CompletedAtUtc = now;

        var job = new ReportJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = tenantContext.OrganizationId ?? inspection.OrganizationId,
            InspectionId = inspectionId,
            IdempotencyKey = idempotencyKey,
            Status = ReportJobStatus.Pending,
            Attempts = 0,
            MaxAttempts = 3,
            CreatedAtUtc = now
        };

        dbContext.ReportJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.PublishAsync(
            new ReportJobMessage(job.Id, tenantContext.OrganizationId!.Value),
            cancellationToken);

        return new CompleteInspectionResult.Created(job.Id);
    }
}

public abstract record CompleteInspectionResult
{
    public sealed record Created(Guid ReportJobId) : CompleteInspectionResult;
    public sealed record AlreadyInProgress(Guid ExistingReportJobId) : CompleteInspectionResult;
    public sealed record NotFound : CompleteInspectionResult;
    public sealed record InvalidState : CompleteInspectionResult;
}
