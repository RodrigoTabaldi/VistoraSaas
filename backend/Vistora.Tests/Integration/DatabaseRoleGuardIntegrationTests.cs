using Microsoft.Extensions.Configuration;
using Vistora.Infrastructure.Persistence.PostgreSql;
using Xunit;

namespace Vistora.Tests.Integration;

[Collection(PostgreSqlCollection.Name)]
public sealed class DatabaseRoleGuardIntegrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Application_role_is_allowed_to_start()
    {
        await GuardFor(fixture.ApplicationConnectionString).StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Superuser_role_is_rejected_before_serving_requests()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            GuardFor(fixture.AdministratorConnectionString).StartAsync(CancellationToken.None));
        Assert.Contains("BYPASSRLS", exception.Message);
    }

    private static DatabaseRoleGuard GuardFor(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString
            })
            .Build();
        return new DatabaseRoleGuard(configuration);
    }
}
