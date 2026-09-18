using Microsoft.EntityFrameworkCore;
using Vistora.Domain;
using Xunit;

namespace Vistora.Tests.Integration;

/// <summary>
/// Validates tenant isolation and concurrency behaviour against a real PostgreSQL instance.
/// These are the guarantees the InMemory-based <see cref="Vistora.Tests.VistoraDbContextTests"/>
/// cannot verify, because InMemory has no RLS, no <c>xmin</c>, no connection pooling, and no
/// transactions - it silently accepts anything relational-specific without actually checking it.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class TenantIsolationIntegrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Row_level_security_hides_rows_from_another_tenant_even_bypassing_the_EF_query_filter()
    {
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        await using (var contextA = fixture.CreateContext(organizationA))
        {
            await SeedOrganizationAsync(contextA, organizationA);
            contextA.Properties.Add(NewProperty());
            await contextA.SaveChangesAsync();
        }

        // IgnoreQueryFilters bypasses the EF Core HasQueryFilter entirely, so if this still
        // returns nothing for organization B, the guarantee is coming from the database-level RLS
        // policy itself - not from the application-side filter, which is exactly what needs
        // proving. The EF filter is a usability nicety; RLS is the actual security boundary.
        await using var contextB = fixture.CreateContext(organizationB);
        var visibleToB = await contextB.Properties.IgnoreQueryFilters().ToListAsync();

        Assert.Empty(visibleToB);
    }

    [Fact]
    public async Task Reusing_a_pooled_connection_for_a_different_tenant_does_not_leak_data()
    {
        // This is the scenario the whole TenantTransactionInterceptor exists for: Npgsql pools
        // physical connections, and ConnectionOpened does not reliably re-fire when a pooled
        // connection is handed to a new logical connection. If tenant isolation depended only on
        // a connection-scoped `set_config`, tenant B could inherit tenant A's session state here.
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var propertyIdA = Guid.NewGuid();

        // Use the SAME connection string / pool across both contexts (default NpgsqlDataSource
        // pooling is keyed by connection string), opening and closing several contexts in
        // sequence to force physical connection reuse from the pool rather than fresh sockets.
        await using (var seedContext = fixture.CreateContext(organizationA))
        {
            await SeedOrganizationAsync(seedContext, organizationA);
        }
        await using (var seedContext = fixture.CreateContext(organizationB))
        {
            await SeedOrganizationAsync(seedContext, organizationB);
        }

        for (var i = 0; i < 5; i++)
        {
            await using var warmup = fixture.CreateContext(organizationA);
            await warmup.Properties.ToListAsync();
        }

        await using (var contextA = fixture.CreateContext(organizationA))
        {
            contextA.Properties.Add(new Property
            {
                Id = propertyIdA,
                Name = "Residencial Aurora",
                Address = "Rua das Flores, 10",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await contextA.SaveChangesAsync();
        }

        // Immediately reuse the pool as tenant B, with no delay - maximises the chance a pooled
        // connection is physically the same one tenant A just used.
        for (var i = 0; i < 10; i++)
        {
            await using var contextB = fixture.CreateContext(organizationB);
            var visibleToB = await contextB.Properties.IgnoreQueryFilters().ToListAsync();
            Assert.DoesNotContain(visibleToB, p => p.Id == propertyIdA);

            // Also verify a write from B in this same (possibly reused) connection is correctly
            // attributed to B, not silently rejected or attributed to A.
            var propertyIdB = Guid.NewGuid();
            contextB.Properties.Add(new Property
            {
                Id = propertyIdB,
                Name = $"Imóvel B {i}",
                Address = "Rua Teste, 1",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await contextB.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Modifying_another_tenants_entity_by_id_is_rejected_not_silently_reassigned()
    {
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var property = NewProperty();

        await using (var contextA = fixture.CreateContext(organizationA))
        {
            await SeedOrganizationAsync(contextA, organizationA);
            contextA.Properties.Add(property);
            await contextA.SaveChangesAsync();
        }

        // Simulate the exact bug class this guard defends against: attaching an entity by Id and
        // marking it Modified without loading it through this context first (which would
        // otherwise make a cross-tenant row unreachable via the query filter).
        await using var contextB = fixture.CreateContext(organizationB);
        await SeedOrganizationAsync(contextB, organizationB);
        var attached = new Property
        {
            Id = property.Id,
            OrganizationId = organizationA, // stale/attacker-controlled value, not B's tenant
            Name = "Nome alterado indevidamente",
            Address = property.Address,
            CreatedAtUtc = property.CreatedAtUtc
        };
        contextB.Attach(attached).State = EntityState.Modified;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => contextB.SaveChangesAsync());
        Assert.Contains("cannot be changed for another organization", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Concurrent_edits_to_the_same_inspection_item_are_detected_via_xmin()
    {
        var organizationId = Guid.NewGuid();
        var (inspectionId, roomId, itemId) = await SeedInspectionAsync(organizationId);

        await using var firstEditor = fixture.CreateContext(organizationId);
        await using var secondEditor = fixture.CreateContext(organizationId);

        var itemAsSeenByFirst = await firstEditor.InspectionItems.SingleAsync(i => i.Id == itemId);
        var itemAsSeenBySecond = await secondEditor.InspectionItems.SingleAsync(i => i.Id == itemId);

        itemAsSeenByFirst.Response = "Parede sem avarias";
        await firstEditor.SaveChangesAsync();

        itemAsSeenBySecond.Response = "Parede com trinca";

        // Both inspectors loaded the same row before either wrote. The second SaveChanges must
        // fail loudly (DbUpdateConcurrencyException) instead of silently overwriting the first
        // inspector's field observation - this is exactly the RF-02 "draft resumed without losing
        // already-persisted items" risk in concurrent field use.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondEditor.SaveChangesAsync());
    }

    [Fact]
    public async Task Writing_without_any_tenant_in_context_is_rejected_even_inside_a_real_transaction()
    {
        await using var context = fixture.CreateContext(organizationId: null);
        context.Properties.Add(NewProperty());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Contains("tenant context", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Property NewProperty() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Residencial Aurora",
        Address = "Rua das Flores, 10",
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    /// <summary>
    /// Every tenant-scoped table has a mandatory FK to <c>organizations</c>, so every test that
    /// writes tenant data must ensure the organization row exists first - RLS and the tenant
    /// context only decide who can see/write a row, they do not create the organization itself.
    /// </summary>
    private static async Task SeedOrganizationAsync(Vistora.Infrastructure.Persistence.PostgreSql.VistoraDbContext context, Guid organizationId)
    {
        context.Organizations.Add(new Organization
        {
            Id = organizationId,
            Name = $"Organização {organizationId:N}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private async Task<(Guid InspectionId, Guid RoomId, Guid ItemId)> SeedInspectionAsync(Guid organizationId)
    {
        await using var context = fixture.CreateContext(organizationId);
        await SeedOrganizationAsync(context, organizationId);

        var property = NewProperty();
        var unit = new Unit { Id = Guid.NewGuid(), PropertyId = property.Id, Identifier = "Apto 101" };
        var inspection = new Inspection
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            Type = InspectionType.MoveIn,
            Status = InspectionStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        var room = new InspectionRoom { Id = Guid.NewGuid(), InspectionId = inspection.Id, Name = "Sala", Position = 1 };
        var item = new InspectionItem
        {
            Id = Guid.NewGuid(),
            InspectionRoomId = room.Id,
            Description = "Estado da pintura",
            Position = 1
        };

        context.Properties.Add(property);
        context.Units.Add(unit);
        context.Inspections.Add(inspection);
        context.InspectionRooms.Add(room);
        context.InspectionItems.Add(item);
        await context.SaveChangesAsync();

        return (inspection.Id, room.Id, item.Id);
    }
}