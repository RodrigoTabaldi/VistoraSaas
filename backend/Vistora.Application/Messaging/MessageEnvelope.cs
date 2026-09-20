namespace Vistora.Application.Messaging;

public sealed record MessageEnvelope(
    Guid Id,
    string Type,
    string Payload,
    DateTimeOffset CreatedAtUtc);

public interface IMessageBus
{
    Task PublishAsync(MessageEnvelope message, CancellationToken cancellationToken = default);
}
