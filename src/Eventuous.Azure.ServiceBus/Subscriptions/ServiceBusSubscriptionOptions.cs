using Eventuous.Azure.ServiceBus.Shared;
using Eventuous.Subscriptions;

namespace Eventuous.Azure.ServiceBus.Subscriptions;

public record ServiceBusSubscriptionOptions : SubscriptionOptions
{
    public required string QueueOrTopicName { get; set; }
    public int MaxConcurrentCalls { get; set; }
    public int PrefetchCount { get; set; }
    public ServiceBusProcessorOptions ProcessorOptions { get; set; } = new();
    public ServiceBusMessageAttributes Attributes { get; init; } = new();
    public Func<ProcessErrorEventArgs, Task>? ErrorHandler { get; init; }
}