using System.Buffers.Binary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vistora.Application.Messaging;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Infrastructure.Messaging;

public sealed class OutboxDispatcher(IServiceScopeFactory scopes, IMessageBus bus, ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var inventory = scopes.CreateAsyncScope();
                var db = inventory.ServiceProvider.GetRequiredService<VistoraDbContext>();
                var organizations = await db.Organizations.AsNoTracking().Select(x => x.Id).ToListAsync(stoppingToken);
                foreach (var organizationId in organizations)
                {
                    await using var scope = scopes.CreateAsyncScope();
                    scope.ServiceProvider.GetRequiredService<TenantContext>().SetOrganization(organizationId);
                    var tenantDb = scope.ServiceProvider.GetRequiredService<VistoraDbContext>();
                    var messages = await tenantDb.OutboxMessages.Where(x => x.PublishedAtUtc == null)
                        .OrderBy(x => x.CreatedAtUtc).Take(100).ToListAsync(stoppingToken);
                    foreach (var message in messages)
                    {
                        await using var transaction = await tenantDb.Database.BeginTransactionAsync(stoppingToken);
                        var lockId = BinaryPrimitives.ReadInt64BigEndian(message.Id.ToByteArray());
                        var acquired = await tenantDb.Database.SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({lockId}) AS \"Value\"").SingleAsync(stoppingToken);
                        if (!acquired) continue;
                        await tenantDb.Entry(message).ReloadAsync(stoppingToken);
                        if (message.PublishedAtUtc is not null) continue;
                        await bus.PublishAsync(new MessageEnvelope(message.Id, message.Type, message.Payload, message.CreatedAtUtc), stoppingToken);
                        message.PublishedAtUtc = DateTimeOffset.UtcNow;
                        await tenantDb.SaveChangesAsync(stoppingToken);
                        await transaction.CommitAsync(stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Outbox dispatch failed; persisted messages will be retried."); }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
