using Vistora.Application.Idempotency;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Vistora.Application.Persistence;
using Vistora.Domain;

namespace Vistora.Application.UseCases;

public sealed class CompleteInspectionUseCase(
    IVistoraDbContext dbContext,
    ITenantContext tenantContext)
{
    public async Task<CompleteInspectionResult> ExecuteAsync(
        Guid inspectionId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var inspection = await dbContext.Inspections
            .Include(x => x.Rooms).ThenInclude(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == inspectionId, cancellationToken);

        if (inspection is null)
        {
            return new CompleteInspectionResult.NotFound();
        }

        if (inspection.Status is InspectionStatus.Completed or InspectionStatus.Approved)
        {
            return new CompleteInspectionResult.InvalidState();
        }

        var items = inspection.Rooms.SelectMany(x => x.Items).ToList();
        if (!InspectionWorkflow.CanComplete(items.Count, items.Count(x => InspectionWorkflow.IsAnswered(x.Response))))
            return new CompleteInspectionResult.IncompleteChecklist();

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

        return new CompleteInspectionResult.Created(job.Id);
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$result")]
[JsonDerivedType(typeof(CompleteInspectionResult.Created), "Created")]
[JsonDerivedType(typeof(CompleteInspectionResult.AlreadyInProgress), "AlreadyInProgress")]
[JsonDerivedType(typeof(CompleteInspectionResult.NotFound), "NotFound")]
[JsonDerivedType(typeof(CompleteInspectionResult.InvalidState), "InvalidState")]
[JsonDerivedType(typeof(CompleteInspectionResult.IncompleteChecklist), "IncompleteChecklist")]
public abstract record CompleteInspectionResult : IIdempotencyResult
{
    [JsonIgnore]
    public bool IsSuccessful => this is Created or AlreadyInProgress;
    public sealed record Created(Guid ReportJobId) : CompleteInspectionResult;
    public sealed record AlreadyInProgress(Guid ExistingReportJobId) : CompleteInspectionResult;
    public sealed record NotFound : CompleteInspectionResult;
    public sealed record InvalidState : CompleteInspectionResult;
    public sealed record IncompleteChecklist : CompleteInspectionResult;
}
