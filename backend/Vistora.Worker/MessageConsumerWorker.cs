using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Vistora.Application.Caching;
using Vistora.Application.Messaging;
using Vistora.Infrastructure.Messaging;

namespace Vistora.Worker;

public sealed class MessageConsumerWorker(
    IConnection connection,
    IApplicationCache cache,
    IOptions<RabbitMqOptions> options,
    ILogger<MessageConsumerWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await using var publisherChannel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            stoppingToken);
        await channel.QueueDeclareAsync(_options.QueueName, durable: true, exclusive: false, autoDelete: false,
            arguments: null, cancellationToken: stoppingToken);
        await DeclareFailureQueuesAsync(channel, stoppingToken);
        await channel.BasicQosAsync(0, 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            MessageEnvelope message;
            try
            {
                message = JsonSerializer.Deserialize<MessageEnvelope>(eventArgs.Body.Span, JsonOptions)
                    ?? throw new JsonException("Message envelope cannot be null.");
            }
            catch (JsonException exception)
            {
                await RouteFailureAsync(publisherChannel, channel, eventArgs, failed: true, 0, exception, stoppingToken);
                return;
            }

            try
            {
                await cache.SetAsync($"vistora:message:{message.Id}", message.Payload, TimeSpan.FromHours(1), stoppingToken);
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, stoppingToken);
                logger.LogInformation("Processed message {MessageId} of type {MessageType}.", message.Id, message.Type);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                var retryCount = MessageRetryPolicy.GetRetryCount(eventArgs.BasicProperties.Headers);
                var failed = !MessageRetryPolicy.ShouldRetry(retryCount);
                await RouteFailureAsync(publisherChannel, channel, eventArgs, failed, retryCount, exception, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(_options.QueueName, autoAck: false, consumer, stoppingToken);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    // Publica numa fila durável antes de confirmar a mensagem original.
    private async Task RouteFailureAsync(
        IChannel publisherChannel,
        IChannel consumerChannel,
        BasicDeliverEventArgs eventArgs,
        bool failed,
        int retryCount,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var nextRetryCount = failed ? retryCount : retryCount + 1;
        var destination = failed ? FailedQueueName : RetryQueueName;
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = eventArgs.BasicProperties.ContentType,
            MessageId = eventArgs.BasicProperties.MessageId,
            Headers = new Dictionary<string, object?>
            {
                [MessageRetryPolicy.RetryCountHeader] = nextRetryCount,
                ["vistora-failure-type"] = exception.GetType().Name
            }
        };

        try
        {
            await publisherChannel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: destination,
                mandatory: true,
                basicProperties: properties,
                body: eventArgs.Body,
                cancellationToken);
        }
        catch (Exception publishException)
        {
            logger.LogError(publishException,
                "Could not route message {MessageId}; keeping it available for redelivery.",
                eventArgs.BasicProperties.MessageId);
            await consumerChannel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: true,
                CancellationToken.None);
            return;
        }

        await consumerChannel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken);
        logger.LogWarning(exception,
            "Message {MessageId} was routed to {Destination} after processing failed.",
            eventArgs.BasicProperties.MessageId,
            destination);
    }

    // Filas separadas preservam os argumentos da fila principal já existente.
    private async Task DeclareFailureQueuesAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(
            RetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = (int)MessageRetryPolicy.RetryDelay.TotalMilliseconds,
                ["x-dead-letter-exchange"] = string.Empty,
                ["x-dead-letter-routing-key"] = _options.QueueName
            },
            cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            FailedQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
    }

    private string RetryQueueName => $"{_options.QueueName}.retry";
    private string FailedQueueName => $"{_options.QueueName}.failed";
}
