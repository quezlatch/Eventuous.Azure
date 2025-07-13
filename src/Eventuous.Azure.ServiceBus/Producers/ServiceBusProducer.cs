using System.Runtime.CompilerServices;
using Eventuous.Producers;
using Eventuous.Producers.Diagnostics;

namespace Eventuous.Azure.ServiceBus.Producers;

public class ServiceBusProducer : BaseProducer<ServiceBusProduceOptions>, IHostedProducer, IAsyncDisposable
{
    // maybe want something a bit more focused on Azure Service Bus?
    static readonly ProducerTracingOptions TracingOptions = new() { MessagingSystem = "azure-service-bus", DestinationKind = "topic", ProduceOperation = "publish" };
    private readonly ServiceBusClient client;
    private readonly ServiceBusProducerOptions options;
    private readonly IEventSerializer serializer;
    private readonly ILogger<ServiceBusProducer>? log;
    private readonly ServiceBusSender sender;

    public ServiceBusProducer(
        ServiceBusClient client,
        ServiceBusProducerOptions options,
        IEventSerializer? serializer = null,
        ILogger<ServiceBusProducer>? log = null) : base(TracingOptions)
    {
        this.client = client;
        this.options = options;
        this.serializer = serializer ?? DefaultEventSerializer.Instance;
        this.log = log;
        this.sender = client.CreateSender(options.QueueOrTopicName, options.SenderOptions);
        log?.LogInformation("ServiceBusProducer created for {QueueOrTopicName}", options.QueueOrTopicName);
    }

    public bool Ready { get; private set; }

    public async ValueTask DisposeAsync()
    {
        await sender.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Ready = true;
        log?.LogInformation("ServiceBusProducer started for {QueueOrTopicName}", options.QueueOrTopicName);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Ready = false;
        await sender.DisposeAsync();
        log?.LogInformation("ServiceBusProducer stopped for {QueueOrTopicName}", options.QueueOrTopicName);
    }

    protected override async Task ProduceMessages(StreamName stream, IEnumerable<ProducedMessage> messages, ServiceBusProduceOptions? options, CancellationToken cancellationToken = default)
    {
        await foreach (var batch in CreateMessageBatches(messages, stream, options, cancellationToken))
        {
            log?.LogInformation("Sending batch of {MessageCount} messages to {QueueOrTopicName}", batch.Count, this.options.QueueOrTopicName);
            try
            {
                await sender.SendMessagesAsync(batch, cancellationToken);
                log?.LogInformation("Batch sent successfully to {QueueOrTopicName}", this.options.QueueOrTopicName);
            }
            catch (Exception ex)
            {
                log?.LogError(ex, "Failed to send batch to {QueueOrTopicName}", this.options.QueueOrTopicName);
                throw;
            }
        }
        ;
    }

    private async IAsyncEnumerable<ServiceBusMessageBatch> CreateMessageBatches(IEnumerable<ProducedMessage> messages, StreamName stream, ServiceBusProduceOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messageBuilder = new ServiceBusMessageBuilder(serializer, stream, options);
        using var enumerator = messages.GetEnumerator();
        while (enumerator.MoveNext())
        {
            using var batch = await sender.CreateMessageBatchAsync();
            var message = messageBuilder.CreateServiceBusMessage(enumerator.Current);
            while (batch.TryAddMessage(message) && enumerator.MoveNext())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    log?.LogWarning("Message production cancelled for {QueueOrTopicName}", this.options.QueueOrTopicName);
                    yield break;
                }
                message = messageBuilder.CreateServiceBusMessage(enumerator.Current);
            }
            yield return batch;
        }
    }
}
