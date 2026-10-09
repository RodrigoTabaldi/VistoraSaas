using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Vistora.Infrastructure.Persistence.PostgreSql;

public static class PostgreSqlConnectionConfiguration
{
    public static string Build(string connectionString, IConfiguration configuration)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var maximumPoolSize = configuration.GetValue<int?>("Database:MaximumPoolSize");
        if (maximumPoolSize.HasValue)
        {
            if (maximumPoolSize.Value < 1 || maximumPoolSize.Value < builder.MinPoolSize)
            {
                throw new InvalidOperationException("Database:MaximumPoolSize must be positive and at least Minimum Pool Size.");
            }

            builder.MaxPoolSize = maximumPoolSize.Value;
        }

        var applicationName = configuration["Database:ApplicationName"];
        if (!string.IsNullOrWhiteSpace(applicationName))
        {
            builder.ApplicationName = applicationName;
        }

        return builder.ConnectionString;
    }
}
