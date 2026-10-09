using Microsoft.Extensions.Configuration;
using Npgsql;
using Vistora.Infrastructure.Persistence.PostgreSql;
using Xunit;

namespace Vistora.Tests;

public sealed class PostgreSqlConnectionConfigurationTests
{
    [Fact]
    public void Host_pool_budget_overrides_connection_string_without_changing_credentials()
    {
        var configuration = Configuration("10");
        var result = new NpgsqlConnectionStringBuilder(PostgreSqlConnectionConfiguration.Build(
            "Host=localhost;Database=vistora;Username=test;Password=test-only;Maximum Pool Size=100", configuration));

        Assert.Equal(10, result.MaxPoolSize);
        Assert.Equal("vistora-worker", result.ApplicationName);
        Assert.True(result.Pooling);
        Assert.Equal("test", result.Username);
        Assert.Equal("test-only", result.Password);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("4")]
    public void Invalid_budget_is_rejected_before_connecting(string maximum)
    {
        Assert.Throws<InvalidOperationException>(() => PostgreSqlConnectionConfiguration.Build(
            "Host=localhost;Minimum Pool Size=5", Configuration(maximum)));
    }

    [Fact]
    public void Missing_host_settings_preserve_explicit_connection_settings()
    {
        var result = new NpgsqlConnectionStringBuilder(PostgreSqlConnectionConfiguration.Build(
            "Host=localhost;Maximum Pool Size=20;Pooling=false;Application Name=custom",
            new ConfigurationBuilder().Build()));

        Assert.Equal(20, result.MaxPoolSize);
        Assert.False(result.Pooling);
        Assert.Equal("custom", result.ApplicationName);
    }

    private static IConfiguration Configuration(string maximum) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:MaximumPoolSize"] = maximum,
            ["Database:ApplicationName"] = "vistora-worker"
        }).Build();
}
