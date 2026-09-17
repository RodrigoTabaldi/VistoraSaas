using Microsoft.EntityFrameworkCore;
using Vistora.Application.Persistence;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;
using Xunit;

namespace Vistora.Tests;

public sealed class VistoraDbContextTests
{
    [Fact]
    public async Task Tenant_filter_hides_records_from_another_organization()
    {
        var databaseName = Guid.NewGuid().ToString();
        var firstOrganization = Guid.NewGuid();
        var secondOrganization = Guid.NewGuid();

        await using (var firstContext = CreateContext(databaseName, firstOrganization))
        {
            firstContext.Properties.Add(new Property
            {
                Id = Guid.NewGuid(),
                Name = "Residencial Aurora",
                Address = "Rua das Flores, 10",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await firstContext.SaveChangesAsync();
        }

        await using var secondContext = CreateContext(databaseName, secondOrganization);
        Assert.Empty(await secondContext.Properties.ToListAsync());
    }

    [Fact]
    public async Task Tenant_scoped_write_without_context_is_rejected()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString(), null);
        context.Properties.Add(new Property
        {
            Id = Guid.NewGuid(),
            Name = "Residencial Aurora",
            Address = "Rua das Flores, 10",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        Assert.Contains("tenant context", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Audit_events_cannot_be_changed_after_insertion()
    {
        var organizationId = Guid.NewGuid();
        await using var context = CreateContext(Guid.NewGuid().ToString(), organizationId);
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "inspection.completed",
            EntityType = "Inspection",
            EntityId = Guid.NewGuid().ToString(),
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        context.AuditEvents.Add(auditEvent);
        await context.SaveChangesAsync();

        auditEvent.EventType = "inspection.approved";
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        Assert.Contains("append-only", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Model_maps_evidence_hash_and_report_version_uniqueness()
    {
        using var context = CreatePostgreSqlContext(Guid.NewGuid());

        var evidence = context.Model.FindEntityType(typeof(Evidence))!;
        var report = context.Model.FindEntityType(typeof(Report))!;

        Assert.Equal("character(64)", evidence.FindProperty(nameof(Evidence.Sha256))!.GetColumnType());
        Assert.Contains(report.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(Report.OrganizationId), nameof(Report.InspectionId), nameof(Report.Version) }));
    }

    private static VistoraDbContext CreateContext(string databaseName, Guid? organizationId)
    {
        var tenantContext = new TestTenantContext(organizationId);
        var options = new DbContextOptionsBuilder<VistoraDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new VistoraDbContext(options, tenantContext);
    }

    private static VistoraDbContext CreatePostgreSqlContext(Guid organizationId)
    {
        var options = new DbContextOptionsBuilder<VistoraDbContext>()
            .UseNpgsql("Host=localhost;Database=vistora;Username=vistora;Password=test-only")
            .Options;

        return new VistoraDbContext(options, new TestTenantContext(organizationId));
    }

    private sealed class TestTenantContext(Guid? organizationId) : ITenantContext
    {
        public Guid? OrganizationId { get; } = organizationId;
    }
}
