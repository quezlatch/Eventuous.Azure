using Azure.Messaging.ServiceBus;

namespace Eventuous.Azure.ServiceBus.Producers;

public class ServiceBusProducerOptions
{
    public required string QueueOrTopicName { get; init; }
    public ServiceBusSenderOptions? SenderOptions { get; init; }
}
