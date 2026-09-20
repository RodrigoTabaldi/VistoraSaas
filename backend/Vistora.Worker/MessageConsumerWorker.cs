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
        await channel.QueueDeclareAsync(_options.QueueName, durable: true, exclusive: false, autoDelete: false,
            arguments: null, cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<MessageEnvelope>(eventArgs.Body.Span, JsonOptions)
                    ?? throw new JsonException("Message envelope cannot be null.");
                await cache.SetAsync($"vistora:message:{message.Id}", message.Payload, TimeSpan.FromHours(1), stoppingToken);
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, stoppingToken);
                logger.LogInformation("Processed message {MessageId} of type {MessageType}.", message.Id, message.Type);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Message processing failed; message will be requeued.");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(_options.QueueName, autoAck: false, consumer, stoppingToken);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
