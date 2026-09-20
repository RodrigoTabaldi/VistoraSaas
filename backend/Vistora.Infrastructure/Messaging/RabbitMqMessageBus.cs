using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Vistora.Application.Messaging;

namespace Vistora.Infrastructure.Messaging;

public sealed class RabbitMqMessageBus(
    IConnection connection,
    IOptions<RabbitMqOptions> options) : IMessageBus
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options = options.Value;

    public async Task PublishAsync(MessageEnvelope message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(_options.QueueName, durable: true, exclusive: false, autoDelete: false,
            arguments: null, cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));
        var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };
        await channel.BasicPublishAsync(string.Empty, _options.QueueName, mandatory: false, properties, body,
            cancellationToken);
    }
}
