using System.Text.Json;
using RabbitMQ.Client;
using Vistora.Application.Messaging;

namespace Vistora.Infrastructure.Messaging;

public sealed class RabbitMqReportJobPublisher(IConnectionFactory connectionFactory) : IReportJobPublisher, IAsyncDisposable
{
    private const string ExchangeName = "vistora.report-jobs";
    private const string RoutingKey = "report-job.created";
    private const string QueuePending = "vistora.report-jobs.pending";

    private const string DlxExchangeName = "vistora.report-jobs.dlx";
    private const string DlxRoutingKey = "report-job.failed";
    private const string DlxQueueFailed = "vistora.report-jobs.failed";

    private IConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public async Task PublishAsync(ReportJobMessage message, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

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

        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _connectionLock.Dispose();
    }
}
