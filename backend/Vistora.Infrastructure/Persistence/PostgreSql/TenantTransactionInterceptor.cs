using System.Data.Common;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vistora.Application.Persistence;

namespace Vistora.Infrastructure.Persistence.PostgreSql;

public sealed class TenantTransactionInterceptor(ITenantContext tenantContext) : DbTransactionInterceptor
{
    public override DbTransaction TransactionStarted(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result)
    {
        SetTenant(connection, result);
        return base.TransactionStarted(connection, eventData, result);
    }

    public override async ValueTask<DbTransaction> TransactionStartedAsync(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result,
        CancellationToken cancellationToken = default)
    {
        await SetTenantAsync(connection, result, cancellationToken);
        return await base.TransactionStartedAsync(connection, eventData, result, cancellationToken);
    }

    private void SetTenant(DbConnection connection, DbTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT set_config('app.organization_id', @organization_id, true)";
        AddTenantParameter(command);
        command.ExecuteNonQuery();
    }

    private async Task SetTenantAsync(DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT set_config('app.organization_id', @organization_id, true)";
        AddTenantParameter(command);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void AddTenantParameter(DbCommand command)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = "organization_id";
        parameter.Value = tenantContext.OrganizationId?.ToString() ?? string.Empty;
        command.Parameters.Add(parameter);
    }
}