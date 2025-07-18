using System.Runtime.CompilerServices;
using Eventuous.Azure.ServiceBus.Shared;
using Eventuous.Producers;

namespace Eventuous.Azure.ServiceBus.Producers;

public class ServiceBusMessageBatchBuilder
{
    private readonly IEventSerializer serializer;
    private readonly ServiceBusMessageAttributes attributes;
    private readonly Action<string>? setActivityMessageType;
    private readonly ServiceBusSender sender;

    public ServiceBusMessageBatchBuilder(ServiceBusSender sender, IEventSerializer? serializer, Shared.ServiceBusMessageAttributes attributes, Action<string>? setActivityMessageType)
    {
        this.sender = sender;
        this.serializer = serializer ?? DefaultEventSerializer.Instance;
        this.attributes = attributes;
        this.setActivityMessageType = setActivityMessageType;
    }

    public async IAsyncEnumerable<(ServiceBusMessageBatch, IList<ProducedMessage>)> CreateMessageBatches(
            IEnumerable<ProducedMessage> messages,
            StreamName stream,
            ServiceBusProduceOptions? options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messageBuilder = new ServiceBusMessageBuilder(serializer, stream, attributes, options, setActivityMessageType);
        using var enumerator = messages.GetEnumerator();
        while (enumerator.MoveNext())
        {
            using var batch = await sender.CreateMessageBatchAsync(cancellationToken);
            var produced = new List<ProducedMessage>();
            var current = enumerator.Current;
            var message = messageBuilder.CreateServiceBusMessage(current);
            while (batch.TryAddMessage(message))
            {
                produced.Add(current);
                if (!enumerator.MoveNext())
                    break;
            }
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            yield return (batch, produced);
        }
    }
}
