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

public abstract class IdempotencyGuard
{
    public abstract Task<IdempotencyOutcome<TResult>> ExecuteAsync<TResult>(
        Guid organizationId,
        string? idempotencyKey,
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default,
        string operationName = "default",
        string requestPayload = "");
}

public interface IIdempotencyResult
{
    bool IsSuccessful { get; }
}
