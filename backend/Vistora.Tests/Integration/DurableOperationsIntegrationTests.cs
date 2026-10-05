using Microsoft.EntityFrameworkCore;
using Vistora.Application.Idempotency;
using Vistora.Domain;
using Vistora.Infrastructure.Idempotency;
using Xunit;

namespace Vistora.Tests.Integration;

[Collection(PostgreSqlCollection.Name)]
public sealed class DurableOperationsIntegrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Committed_operation_replays_without_creating_another_property()
    {
        var tenant = Guid.NewGuid();
        await using var db = fixture.CreateContext(tenant);
        db.Organizations.Add(new Organization { Id = tenant, Name = "Test", CreatedAtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var calls = 0;
        async Task<Guid> Operation()
        {
            calls++;
            var property = new Property { Id = Guid.NewGuid(), Name = "Home", Address = "Street", OrganizationId = tenant };
            db.Properties.Add(property);
            await db.SaveChangesAsync();
            return property.Id;
        }
        var guard = new PostgreSqlIdempotencyGuard(db);
        var first = await guard.ExecuteAsync(tenant, "key", Operation, operationName: "create", requestPayload: "address");
        var replay = await guard.ExecuteAsync(tenant, "key", Operation, operationName: "create", requestPayload: "address");
        Assert.Equal(IdempotencyResultKind.Replayed, replay.Kind);
        Assert.Equal(first.Result, replay.Result);
        Assert.Equal(1, calls);
        Assert.Single(await db.Properties.ToListAsync());
        Assert.Equal(IdempotencyResultKind.Conflict, (await guard.ExecuteAsync(tenant, "key", Operation,
            operationName: "create", requestPayload: "different")).Kind);
        await using var otherTenant = fixture.CreateContext(Guid.NewGuid());
        Assert.Empty(await otherTenant.IdempotencyRecords.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Failure_after_save_rolls_back_and_the_key_can_be_retried()
    {
        var tenant = Guid.NewGuid();
        await using (var db = fixture.CreateContext(tenant))
        {
            db.Organizations.Add(new Organization { Id = tenant, Name = "Test", CreatedAtUtc = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => new PostgreSqlIdempotencyGuard(db).ExecuteAsync<Guid>(tenant, "key", async () =>
            {
                db.Properties.Add(new Property { Id = Guid.NewGuid(), Name = "Home", Address = "Street", OrganizationId = tenant });
                await db.SaveChangesAsync();
                throw new InvalidOperationException("Simulated failure after save");
            }));
        }
        await using var retry = fixture.CreateContext(tenant);
        Assert.Empty(await retry.Properties.ToListAsync());
        Assert.Empty(await retry.IdempotencyRecords.ToListAsync());
        var result = await new PostgreSqlIdempotencyGuard(retry).ExecuteAsync(tenant, "key", () => Task.FromResult(Guid.NewGuid()));
        Assert.Equal(IdempotencyResultKind.Fresh, result.Kind);
    }
    [Fact]
    public async Task Validation_failure_does_not_cache_a_result_or_block_a_corrected_draft()
    {
        var tenant = Guid.NewGuid();
        await using var db = fixture.CreateContext(tenant);
        db.Organizations.Add(new Organization { Id = tenant, Name = "Test", CreatedAtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var guard = new PostgreSqlIdempotencyGuard(db);
        await guard.ExecuteAsync<Vistora.Application.UseCases.CompleteInspectionResult>(tenant, "key",
            () => Task.FromResult<Vistora.Application.UseCases.CompleteInspectionResult>(new Vistora.Application.UseCases.CompleteInspectionResult.IncompleteChecklist()));
        Assert.Empty(await db.IdempotencyRecords.ToListAsync());
        var corrected = await guard.ExecuteAsync<Vistora.Application.UseCases.CompleteInspectionResult>(tenant, "key",
            () => Task.FromResult<Vistora.Application.UseCases.CompleteInspectionResult>(new Vistora.Application.UseCases.CompleteInspectionResult.Created(Guid.NewGuid())));
        Assert.Equal(IdempotencyResultKind.Fresh, corrected.Kind);
        Assert.Single(await db.IdempotencyRecords.ToListAsync());
    }

}
