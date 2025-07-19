using Eventuous.Azure.ServiceBus.Shared;
using Eventuous.Subscriptions;

namespace Eventuous.Azure.ServiceBus.Subscriptions;

public record ServiceBusSubscriptionOptions : SubscriptionOptions
{
    public required IQueueOrTopic QueueOrTopic { get; set; }
    public int MaxConcurrentCalls { get; set; }
    public int PrefetchCount { get; set; }
    public ServiceBusProcessorOptions ProcessorOptions { get; set; } = new();
    public ServiceBusMessageAttributes Attributes { get; init; } = new();
    public Func<ProcessErrorEventArgs, Task>? ErrorHandler { get; init; }
}

public interface IQueueOrTopic
{
    ServiceBusProcessor MakeProcessor(ServiceBusClient client, ServiceBusSubscriptionOptions options);
}
public record Queue(string Name) : IQueueOrTopic
{
    public ServiceBusProcessor MakeProcessor(ServiceBusClient client, ServiceBusSubscriptionOptions options)
    {
        return client.CreateProcessor(Name, options.ProcessorOptions);
    }
}

public record Topic(string Name) : IQueueOrTopic
{
    public ServiceBusProcessor MakeProcessor(ServiceBusClient client, ServiceBusSubscriptionOptions options)
    {
        return client.CreateProcessor(Name, options.SubscriptionId, options.ProcessorOptions);
    }
}

public record TopicAndSubscription(string Name, string Subscription) : IQueueOrTopic
{
    public ServiceBusProcessor MakeProcessor(ServiceBusClient client, ServiceBusSubscriptionOptions options)
    {
        return client.CreateProcessor(Name, Subscription, options.ProcessorOptions);
    }
}