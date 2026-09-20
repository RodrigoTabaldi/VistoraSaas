using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Vistora.Application.Persistence;
using Vistora.Infrastructure.Persistence.PostgreSql;
using Xunit;

namespace Vistora.Tests.Integration;

/// <summary>
/// Boots a real, disposable PostgreSQL container once per test class and applies the actual EF
/// Core migrations against it (not <c>EnsureCreated</c>), so these tests exercise exactly what
/// runs in production: real RLS policies, the real <c>xmin</c> mapping, and the real interceptors
/// - none of which the InMemory provider used by <see cref="Vistora.Tests.VistoraDbContextTests"/>
/// can exercise, since InMemory never talks to PostgreSQL and silently no-ops anything relational.
/// </summary>
/// <remarks>
/// Migrations run as the container's own user ("vistora"), which the official PostgreSQL Docker
/// image always creates as a SUPERUSER. PostgreSQL superusers unconditionally bypass row security
/// - no policy, no FORCE ROW LEVEL SECURITY, and no application-side interceptor changes that.
/// Test contexts must therefore connect as the non-superuser "vistora_app" role created by the
/// CreateApplicationRole migration, exactly as the real API/Worker do via
/// VISTORA_POSTGRES_CONNECTION - otherwise these tests would "prove" isolation that was never
/// actually being enforced, the same way it silently wasn't in production before that migration.
/// </remarks>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("vistora")
        .WithUsername("vistora")
        .WithPassword("test-only")
        .Build();

    private string? _appConnectionString;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply the same migrations the application ships, exactly as `dotnet ef database
        // update` would, so a broken migration or a broken interceptor is caught here instead of
        // in a real environment. This includes CreateApplicationRole, which is what creates
        // vistora_app below - so migrations must run first.
        var superuserConnectionString = _container.GetConnectionString();
        await using var migrationContext = new VistoraDbContext(
            new DbContextOptionsBuilder<VistoraDbContext>().UseNpgsql(superuserConnectionString).Options,
            NoTenant());
        await migrationContext.Database.MigrateAsync();

        // Swap only the credentials, keeping the same host/port/database Testcontainers assigned.
        // Built from the container's own public properties (Hostname / mapped port for the
        // container's internal 5432), not by regex-editing the superuser connection string,
        // since the exact string format Npgsql produces is an implementation detail this
        // shouldn't depend on.
        _appConnectionString =
            $"Host={_container.Hostname};Port={_container.GetMappedPublicPort(5432)};" +
            "Database=vistora;Username=vistora_app;Password=change-me";
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>
    /// Builds a <see cref="VistoraDbContext"/> wired exactly like production: connected as the
    /// non-superuser "vistora_app" role (so RLS is actually enforced, not bypassed), with the same
    /// <see cref="TenantSessionConnectionInterceptor"/>, <see cref="TenantTransactionInterceptor"/>
    /// and <see cref="TenantCommandInterceptor"/> registered in
    /// <c>DependencyInjection.AddVistoraInfrastructure</c>. Using anything less (the superuser
    /// connection, or skipping an interceptor) would mean the test isn't actually exercising the
    /// tenant-isolation mechanism it exists to verify.
    /// </summary>
    public VistoraDbContext CreateContext(Guid? organizationId)
    {
        if (_appConnectionString is null)
        {
            throw new InvalidOperationException("InitializeAsync must run before CreateContext.");
        }

        var tenantContext = new TestTenantContext(organizationId);
        var options = new DbContextOptionsBuilder<VistoraDbContext>()
            .UseNpgsql(_appConnectionString)
            .AddInterceptors(
                new TenantSessionConnectionInterceptor(),
                new TenantTransactionInterceptor(tenantContext),
                new TenantCommandInterceptor(tenantContext))
            .Options;

        return new VistoraDbContext(options, tenantContext);
    }

    private static TestTenantContext NoTenant() => new(null);

    private sealed class TestTenantContext(Guid? organizationId) : ITenantContext
    {
        public Guid? OrganizationId { get; } = organizationId;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSql";
}