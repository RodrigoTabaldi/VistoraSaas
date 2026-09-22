namespace Vistora.Application.Idempotency;

public interface IIdempotencyStore
{
    Task<string?> TryGetResultAsync(Guid organizationId, string idempotencyKey, CancellationToken cancellationToken = default);

    Task StoreResultAsync(Guid organizationId, string idempotencyKey, string serializedResult, CancellationToken cancellationToken = default);

    Task<IdempotencyLeaseResult> TryAcquireAsync(Guid organizationId, string idempotencyKey, CancellationToken cancellationToken = default);
}

public enum IdempotencyLeaseOutcome
{
    Acquired,
    AlreadyCompleted,
    InProgress
}

public sealed record IdempotencyLeaseResult(
    IdempotencyLeaseOutcome Outcome,
    string? ExistingResult);
