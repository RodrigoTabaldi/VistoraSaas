using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Vistora.Infrastructure.Persistence.PostgreSql;

public sealed class TenantSessionConnectionInterceptor : DbConnectionInterceptor
{
    public override InterceptionResult ConnectionClosing(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        ResetTenant(connection);
        return base.ConnectionClosing(connection, eventData, result);
    }

    public override async ValueTask<InterceptionResult> ConnectionClosingAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        await ResetTenantAsync(connection, CancellationToken.None);
        return await base.ConnectionClosingAsync(connection, eventData, result);
    }

    private static void ResetTenant(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "RESET app.organization_id";
        command.ExecuteNonQuery();
    }

    private static async Task ResetTenantAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "RESET app.organization_id";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
