using RabbitMQ.Client;

namespace Vistora.Infrastructure.Messaging;

internal static class RabbitMqReportJobTopology
{
    public const string ExchangeName = "vistora.report-jobs";
    public const string RoutingKey = "report-job.created";
    public const string QueuePending = "vistora.report-jobs.pending";

    private const string DlxExchangeName = "vistora.report-jobs.dlx";
    private const string DlxRoutingKey = "report-job.failed";
    private const string DlxQueueFailed = "vistora.report-jobs.failed";

    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: DlxExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: DlxQueueFailed,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: DlxQueueFailed,
            exchange: DlxExchangeName,
            routingKey: DlxRoutingKey,
            cancellationToken: cancellationToken);

        var pendingQueueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = DlxExchangeName,
            ["x-dead-letter-routing-key"] = DlxRoutingKey
        };

        await channel.QueueDeclareAsync(
            queue: QueuePending,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: pendingQueueArgs,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: QueuePending,
            exchange: ExchangeName,
            routingKey: RoutingKey,
            cancellationToken: cancellationToken);
    }
}
