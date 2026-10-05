using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vistora.Application.Idempotency;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Infrastructure.Idempotency;

public sealed class PostgreSqlIdempotencyGuard(VistoraDbContext db) : IdempotencyGuard
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public override async Task<IdempotencyOutcome<TResult>> ExecuteAsync<TResult>(
        Guid organizationId, string? idempotencyKey, Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default, string operationName = "default", string requestPayload = "")
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return IdempotencyOutcome<TResult>.Fresh(await operation());

        var keyHash = Hash(operationName + ":" + idempotencyKey);
        var requestHash = Hash(requestPayload);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // The transaction owns the lock: process crashes and rollbacks release it automatically.
        var lockBytes = SHA256.HashData(Encoding.UTF8.GetBytes(organizationId.ToString("N") + keyHash));
        var lockId = BinaryPrimitives.ReadInt64BigEndian(lockBytes);
        var acquired = await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({lockId}) AS \"Value\"")
            .SingleAsync(cancellationToken);
        if (!acquired) return IdempotencyOutcome<TResult>.Conflict();

        var existing = await db.IdempotencyRecords.AsNoTracking()
            .SingleOrDefaultAsync(x => x.KeyHash == keyHash, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != requestHash) return IdempotencyOutcome<TResult>.Conflict();
            var replay = JsonSerializer.Deserialize<TResult>(existing.ResultJson, JsonOptions)
                ?? throw new InvalidOperationException("Stored operation result is invalid.");
            return IdempotencyOutcome<TResult>.Replayed(replay);
        }

        try
        {
            var result = await operation();
            // Validation failures must remain retryable after the user corrects the draft.
            if (result is IIdempotencyResult { IsSuccessful: false }) return IdempotencyOutcome<TResult>.Fresh(result);
            db.IdempotencyRecords.Add(new IdempotencyRecord
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, KeyHash = keyHash,
                RequestHash = requestHash, ResultJson = JsonSerializer.Serialize(result, JsonOptions),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return IdempotencyOutcome<TResult>.Fresh(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return IdempotencyOutcome<TResult>.Conflict();
        }
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
