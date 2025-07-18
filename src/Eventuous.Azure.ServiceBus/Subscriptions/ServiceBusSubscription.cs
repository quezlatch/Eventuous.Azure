using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Context;
using Eventuous.Subscriptions.Filters;
using Microsoft.Extensions.Logging;

namespace Eventuous.Azure.ServiceBus.Subscriptions;

public class ServiceBusSubscription : EventSubscription<ServiceBusSubscriptionOptions>
{
    private readonly ServiceBusClient client;
    private readonly Func<ProcessErrorEventArgs, Task> defaultErrorHandler;
    private ServiceBusProcessor? processor;

    public ServiceBusSubscription(ServiceBusClient client, ServiceBusSubscriptionOptions options, ConsumePipe consumePipe, ILoggerFactory? loggerFactory, IEventSerializer? eventSerializer) :
     base(options, consumePipe, loggerFactory, eventSerializer)
    {
        this.client = client;
        this.defaultErrorHandler = Options.ErrorHandler ?? DefaultErrorHandler;
    }

    protected override ValueTask Subscribe(CancellationToken cancellationToken)
    {
        processor = client.CreateProcessor(Options.QueueOrTopicName, Options.SubscriptionId, Options.ProcessorOptions);
        processor.ProcessMessageAsync += HandleMessage;

        processor.ProcessErrorAsync += defaultErrorHandler;
        return new ValueTask(processor.StartProcessingAsync(cancellationToken));

        async Task HandleMessage(ProcessMessageEventArgs arg)
        {
            CancellationToken ct = arg.CancellationToken;
            if (ct.IsCancellationRequested) return;
            var msg = arg.Message;
            var eventType = msg.ApplicationProperties[Options.Attributes.EventType].ToString()
            ?? throw new InvalidOperationException("Event type is missing in message properties");
            var contentType = msg.ContentType;
            // Should this be a stream name? or topic or something
            var streamName = msg.ApplicationProperties[Options.Attributes.StreamName].ToString()
            ?? throw new InvalidOperationException("Stream name is missing in message properties");

            var evt = DeserializeData(contentType, eventType, msg.Body, streamName);

            var ctx = new MessageConsumeContext(
    msg.MessageId,
    eventType,
    contentType,
streamName,
    0,
    0,
    0,
    Sequence++,
    msg.EnqueuedTime.UtcDateTime,
    evt,
    AsMeta(msg.ApplicationProperties),
    SubscriptionId,
    ct
);

            try
            {
                await Handler(ctx);
            }
            catch (Exception ex) { await defaultErrorHandler(new(ex, ServiceBusErrorSource.Abandon, arg.FullyQualifiedNamespace, arg.EntityPath, arg.Identifier, arg.CancellationToken)); }
        }
    }

    private Metadata? AsMeta(IReadOnlyDictionary<string, object> applicationProperties) =>
        new(applicationProperties.ToDictionary(pair => pair.Key, pair => (object?)pair.Value));

    private async Task DefaultErrorHandler(ProcessErrorEventArgs arg)
    {
        // Log the error
        Log.ErrorLog?.Log(arg.Exception, "Error processing message: {Identifier}", arg.Identifier);

        // Optionally, you can handle the error further, e.g., by sending to a dead-letter queue
        await Task.CompletedTask;
    }

    protected override ValueTask Unsubscribe(CancellationToken cancellationToken)
    {
        return new ValueTask(processor?.StopProcessingAsync(cancellationToken) ?? Task.CompletedTask);
    }
}
