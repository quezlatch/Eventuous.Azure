using Eventuous.Producers;

namespace Eventuous.Azure.ServiceBus.Producers;

public class ServiceBusMessageBuilder
{
    private static readonly HashSet<string> ReservedAttributes = new(StringComparer.OrdinalIgnoreCase)
        {
            MetaTags.MessageId,
            MetaTags.CorrelationId,
        };

    private readonly IEventSerializer serializer;
    private readonly string streamName;
    private readonly ServiceBusProduceOptions? options;
    private readonly Action<string>? setActivityMessageType;

    public ServiceBusMessageBuilder(IEventSerializer serializer, string streamName, ServiceBusProduceOptions? options = null, Action<string>? setActivityMessageType = null)
    {
        this.serializer = serializer;
        this.streamName = streamName;
        this.options = options;
        this.setActivityMessageType = setActivityMessageType;
    }
    public ServiceBusMessage CreateServiceBusMessage(ProducedMessage message)
    {
        var (messageType, contentType, payload) = serializer.SerializeEvent(message.Message);
        setActivityMessageType?.Invoke(messageType);
        IEnumerable<KeyValuePair<string, object?>> applicationProperties = GetCustomApplicationProperties(message, messageType);

        var serviceBusMessage = new ServiceBusMessage(payload)
        {
            ContentType = contentType,
            MessageId = message.MessageId.ToString(),
            Subject = options?.Subject,
            TimeToLive = options?.TimeToLive ?? TimeSpan.MaxValue,
            CorrelationId = message.Metadata?.GetCorrelationId(),
            To = options?.To,
            ReplyTo = options?.ReplyTo
        };

        foreach (var property in applicationProperties)
        {
            serviceBusMessage.ApplicationProperties.Add(property);
        }

        return serviceBusMessage;
    }

    private IEnumerable<KeyValuePair<string, object?>> GetCustomApplicationProperties(ProducedMessage message, string messageType) =>
     (message.Metadata ?? [])
        .Concat(message.AdditionalHeaders ?? [])
        .Concat(
        [
            new (ServiceBusMessageAttributes.EventType, messageType),
            new (ServiceBusMessageAttributes.StreamName, streamName),
        ])
        .Where(pair => !ReservedAttributes.Contains(pair.Key));
}
