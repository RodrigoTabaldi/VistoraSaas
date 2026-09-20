namespace Vistora.Application.Messaging;

/// <summary>
/// Publishes a lightweight notification that a ReportJob is ready to be processed. The message
/// payload is intentionally minimal (just the ids) - the Worker fetches the current state from
/// PostgreSQL, which remains the source of truth (per ADR-002), rather than trusting a payload
/// that could be stale by the time the Worker consumes it.
/// </summary>
public interface IReportJobPublisher
{
    Task PublishAsync(ReportJobMessage message, CancellationToken cancellationToken = default);
}

public sealed record ReportJobMessage(Guid ReportJobId, Guid OrganizationId);
