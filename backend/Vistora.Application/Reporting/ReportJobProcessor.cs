using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vistora.Application.Persistence;
using Vistora.Application.Storage;
using Vistora.Domain;

namespace Vistora.Application.Reporting;

public sealed class ReportJobProcessor(
    IVistoraDbContext dbContext,
    IReportPdfGenerator pdfGenerator,
    IPrivateObjectStorage storage,
    ILogger<ReportJobProcessor> logger)
{
    public async Task ProcessAsync(Guid reportJobId, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.ReportJobs
            .FirstOrDefaultAsync(x => x.Id == reportJobId, cancellationToken);

        if (job is null)
        {
            logger.LogWarning("ReportJob with ID '{ReportJobId}' was not found.", reportJobId);
            return;
        }

        if (job.Status is ReportJobStatus.Completed or ReportJobStatus.Failed)
        {
            logger.LogWarning(
                "ReportJob '{ReportJobId}' has already been processed with status '{Status}'. Ignoring duplicate queue message.",
                reportJobId,
                job.Status);
            return;
        }

        job.Status = ReportJobStatus.Processing;
        job.StartedAtUtc = DateTimeOffset.UtcNow;
        job.Attempts += 1;

        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var pdf = await pdfGenerator.GenerateAsync(job.InspectionId, cancellationToken);

            var maxVersion = await dbContext.Reports
                .Where(r => r.InspectionId == job.InspectionId)
                .Select(r => (int?)r.Version)
                .MaxAsync(cancellationToken) ?? 0;

            var nextVersion = maxVersion + 1;
            var objectKey = $"reports/{job.OrganizationId}/{job.InspectionId}/{nextVersion}.pdf";

            await using (var stream = new MemoryStream(pdf.Content))
            {
                var upload = new StorageUpload(
                    ObjectKey: objectKey,
                    Content: stream,
                    ContentType: "application/pdf",
                    ContentLength: pdf.Content.Length);

                await storage.UploadAsync(upload, cancellationToken);
            }

            var now = DateTimeOffset.UtcNow;
            var report = new Report
            {
                Id = Guid.NewGuid(),
                OrganizationId = job.OrganizationId,
                InspectionId = job.InspectionId,
                Version = nextVersion,
                ObjectKey = objectKey,
                Sha256 = pdf.Sha256Hex,
                CreatedAtUtc = now
            };

            job.Status = ReportJobStatus.Completed;
            job.CompletedAtUtc = now;
            job.ResultReportId = report.Id;

            var auditEvent = new AuditEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = job.OrganizationId,
                ActorUserId = null,
                EventType = "ReportGenerated",
                EntityType = "Report",
                EntityId = report.Id.ToString(),
                OccurredAtUtc = now,
                CorrelationId = reportJobId.ToString()
            };

            dbContext.Reports.Add(report);
            dbContext.AuditEvents.Add(auditEvent);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing ReportJob '{ReportJobId}'", reportJobId);
            job.ErrorMessage = ex.Message;

            if (job.Attempts < job.MaxAttempts)
            {
                job.Status = ReportJobStatus.Pending;
            }
            else
            {
                job.Status = ReportJobStatus.Failed;
                logger.LogError(
                    ex,
                    "OPERATIONAL ALERT: ReportJob '{ReportJobId}' failed final attempt ({Attempts}/{MaxAttempts})",
                    reportJobId,
                    job.Attempts,
                    job.MaxAttempts);
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                logger.LogError(saveEx, "Failed to persist failure status for ReportJob '{ReportJobId}'", reportJobId);
            }
        }
    }
}
