using Azure.Messaging.ServiceBus;
using Eventuous.Azure.ServiceBus.Shared;

namespace Eventuous.Azure.ServiceBus.Producers;

public class ServiceBusProducerOptions
{
    public required string QueueOrTopicName { get; init; }
    public ServiceBusSenderOptions? SenderOptions { get; init; }

    public ServiceBusMessageAttributes Attributes { get; init; } = new();
}
