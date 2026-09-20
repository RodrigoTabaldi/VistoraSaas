using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vistora.Application.Persistence;

namespace Vistora.Infrastructure.Persistence.PostgreSql;
public sealed class TenantCommandInterceptor(ITenantContext tenantContext) : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ApplyTenant(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        await ApplyTenantAsync(command, cancellationToken);
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyTenant(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await ApplyTenantAsync(command, cancellationToken);
        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        ApplyTenant(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        await ApplyTenantAsync(command, cancellationToken);
        return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void ApplyTenant(DbCommand command)
    {
        var connection = command.Connection;
        if (connection is null)
        {
            return;
        }

        using var tenantCommand = connection.CreateCommand();
       
        tenantCommand.Transaction = command.Transaction;
        tenantCommand.CommandText = "SELECT set_config('app.organization_id', @organization_id, false)";
        AddTenantParameter(tenantCommand);
        tenantCommand.ExecuteNonQuery();
    }

    private async Task ApplyTenantAsync(DbCommand command, CancellationToken cancellationToken)
    {
        var connection = command.Connection;
        if (connection is null)
        {
            return;
        }

        await using var tenantCommand = connection.CreateCommand();
        tenantCommand.Transaction = command.Transaction;
        tenantCommand.CommandText = "SELECT set_config('app.organization_id', @organization_id, false)";
        AddTenantParameter(tenantCommand);
        await tenantCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private void AddTenantParameter(DbCommand command)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = "organization_id";
        parameter.Value = tenantContext.OrganizationId?.ToString() ?? string.Empty;
        command.Parameters.Add(parameter);
    }
}