using System.Text.Json;

namespace Vistora.Application.Idempotency;

public enum IdempotencyResultKind
{
    Fresh,
    Replayed,
    Conflict
}

public sealed record IdempotencyOutcome<TResult>(
    IdempotencyResultKind Kind,
    TResult? Result)
{
    public static IdempotencyOutcome<TResult> Fresh(TResult result) =>
        new(IdempotencyResultKind.Fresh, result);

    public static IdempotencyOutcome<TResult> Replayed(TResult result) =>
        new(IdempotencyResultKind.Replayed, result);

    public static IdempotencyOutcome<TResult> Conflict() =>
        new(IdempotencyResultKind.Conflict, default);
}

public sealed class IdempotencyGuard(IIdempotencyStore store)
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IdempotencyOutcome<TResult>> ExecuteAsync<TResult>(
        Guid organizationId,
        string? idempotencyKey,
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var freshResult = await operation();
            return IdempotencyOutcome<TResult>.Fresh(freshResult);
        }

        var lease = await store.TryAcquireAsync(organizationId, idempotencyKey, cancellationToken);

        switch (lease.Outcome)
        {
            case IdempotencyLeaseOutcome.InProgress:
                return IdempotencyOutcome<TResult>.Conflict();

            case IdempotencyLeaseOutcome.AlreadyCompleted:
                if (string.IsNullOrEmpty(lease.ExistingResult))
                {
                    throw new InvalidOperationException("Idempotency result was marked as completed but existing payload was empty.");
                }

                var replayedResult = JsonSerializer.Deserialize<TResult>(lease.ExistingResult, WebJsonOptions);
                return IdempotencyOutcome<TResult>.Replayed(replayedResult!);

            case IdempotencyLeaseOutcome.Acquired:
                var result = await operation();
                var serialized = JsonSerializer.Serialize(result, WebJsonOptions);
                await store.StoreResultAsync(organizationId, idempotencyKey, serialized, cancellationToken);
                return IdempotencyOutcome<TResult>.Fresh(result);

            default:
                throw new InvalidOperationException($"Unexpected lease outcome: {lease.Outcome}");
        }
    }
}
