using StackExchange.Redis;
using Vistora.Application.Idempotency;

namespace Vistora.Infrastructure.Idempotency;

public sealed class RedisIdempotencyStore(IConnectionMultiplexer connectionMultiplexer) : IIdempotencyStore
{
    private const string InProgressSentinel = "\u0000IN_PROGRESS\u0000";
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    public async Task<IdempotencyLeaseResult> TryAcquireAsync(
        Guid organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(organizationId, idempotencyKey);
        var db = connectionMultiplexer.GetDatabase();

        bool acquired = await db.StringSetAsync(
            key,
            InProgressSentinel,
            Ttl,
            When.NotExists);

        if (acquired)
        {
            return new IdempotencyLeaseResult(IdempotencyLeaseOutcome.Acquired, null);
        }

        RedisValue existingValue = await db.StringGetAsync(key);
        if (existingValue == InProgressSentinel)
        {
            return new IdempotencyLeaseResult(IdempotencyLeaseOutcome.InProgress, null);
        }

        return new IdempotencyLeaseResult(IdempotencyLeaseOutcome.AlreadyCompleted, existingValue.ToString());
    }

    public async Task StoreResultAsync(
        Guid organizationId,
        string idempotencyKey,
        string serializedResult,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(organizationId, idempotencyKey);
        var db = connectionMultiplexer.GetDatabase();

        await db.StringSetAsync(key, serializedResult, Ttl);
    }

    public async Task<string?> TryGetResultAsync(
        Guid organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(organizationId, idempotencyKey);
        var db = connectionMultiplexer.GetDatabase();

        RedisValue value = await db.StringGetAsync(key);
        if (!value.HasValue || value == InProgressSentinel)
        {
            return null;
        }

        return value.ToString();
    }

    private static string GetKey(Guid organizationId, string idempotencyKey) =>
        $"vistora:idempotency:{organizationId:N}:{idempotencyKey}";
}
