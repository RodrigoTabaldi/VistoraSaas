namespace Vistora.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "Messaging:RabbitMq";

    public string QueueName { get; init; } = "vistora.messages";
}
