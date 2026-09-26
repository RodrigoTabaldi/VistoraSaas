using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Vistora.Application.Messaging;
using Vistora.Application.Reporting;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Infrastructure.Messaging;

public sealed class ReportJobConsumer(
    IConnectionFactory connectionFactory,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ReportJobConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await connectionFactory.CreateConnectionAsync(stoppingToken);
                var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await RabbitMqReportJobTopology.DeclareAsync(channel, stoppingToken);

                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var deliveryTag = ea.DeliveryTag;

                    ReportJobMessage? message = null;
                    try
                    {
                        message = JsonSerializer.Deserialize<ReportJobMessage>(body);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to deserialize ReportJobMessage payload from queue.");
                        await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                        return;
                    }

                    if (message is null)
                    {
                        logger.LogError("Received null ReportJobMessage payload from queue.");
                        await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                        return;
                    }

                    try
                    {
                        await using var scope = serviceScopeFactory.CreateAsyncScope();

                        var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
                        tenantContext.SetOrganization(message.OrganizationId);

                        var processor = scope.ServiceProvider.GetRequiredService<ReportJobProcessor>();
                        await processor.ProcessAsync(message.ReportJobId, stoppingToken);

                        await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Unhandled exception while processing queue message for ReportJob '{ReportJobId}'", message.ReportJobId);
                        await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: RabbitMqReportJobTopology.QueuePending,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                logger.LogInformation("ReportJobConsumer started listening on '{Queue}'.", RabbitMqReportJobTopology.QueuePending);

                var tcs = new TaskCompletionSource();
                using var reg = stoppingToken.Register(() => tcs.TrySetResult());
                await tcs.Task;

                await channel.CloseAsync(cancellationToken: CancellationToken.None);
                await connection.CloseAsync(cancellationToken: CancellationToken.None);
                break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to connect or consume from RabbitMQ. Retrying in 5 seconds...");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
