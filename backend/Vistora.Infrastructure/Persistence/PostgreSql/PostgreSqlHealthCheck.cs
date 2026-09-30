using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Vistora.Infrastructure.Persistence.PostgreSql;

public sealed class PostgreSqlHealthCheck(string connectionString) : IHealthCheck
{
    private readonly string _connectionString = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Timeout = 3,
        CommandTimeout = 3
    }.ConnectionString;

    // Uma consulta simples confirma se o PostgreSQL aceita conexões da aplicação.
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("PostgreSQL is reachable.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unavailable.", exception);
        }
    }
}
