using Microsoft.EntityFrameworkCore;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;
using Xunit;

namespace Vistora.Tests;

public sealed class InspectionReadQueriesTests
{
    [Fact]
    public void Paginated_summary_can_be_translated_to_PostgreSQL_without_loading_rooms()
    {
        using var db = new VistoraDbContext(new DbContextOptionsBuilder<VistoraDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_only").Options, Tenant());
        var sql = InspectionReadQueries.Summaries(db.Inspections.OrderBy(x => x.CreatedAtUtc).Skip(20).Take(20)).ToQueryString();
        Assert.Contains("LIMIT", sql);
        Assert.Contains("OFFSET", sql);
        Assert.Contains("inspection_items", sql);
    }

    [Fact]
    public void Statistics_are_aggregated_in_PostgreSQL_instead_of_in_browser_memory()
    {
        using var db = new VistoraDbContext(new DbContextOptionsBuilder<VistoraDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_only").Options, Tenant());
        var sql = InspectionReadQueries.Statistics(db.Inspections).ToQueryString();
        Assert.Contains("GROUP BY", sql);
        Assert.Contains("reports", sql);
    }
    [Fact]
    public void Missing_tenant_produces_a_guarded_query_instead_of_nullable_parameter_failure()
    {
        using var db = new VistoraDbContext(new DbContextOptionsBuilder<VistoraDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_only").Options, new TenantContext());
        var sql = InspectionReadQueries.Summaries(db.Inspections.Take(20)).ToQueryString();
        Assert.Contains("WHERE", sql);
    }

    private static TenantContext Tenant()
    {
        var tenant = new TenantContext();
        tenant.SetOrganization(Guid.NewGuid());
        return tenant;
    }
}
