using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vistora.Infrastructure.Persistence.PostgreSql;

/// <summary>Used only by EF tooling; runtime configuration always comes from ConnectionStrings:Postgres.</summary>
public sealed class VistoraDbContextFactory : IDesignTimeDbContextFactory<VistoraDbContext>
{
    public VistoraDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<VistoraDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=vistora;Username=vistora;Password=change-me",
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "public"))
            .Options;

        return new VistoraDbContext(options, new TenantContext());
    }
}
