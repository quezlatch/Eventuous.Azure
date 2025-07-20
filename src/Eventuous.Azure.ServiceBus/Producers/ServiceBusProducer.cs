using System.Runtime.CompilerServices;
using Eventuous.Producers;
using Eventuous.Producers.Diagnostics;

namespace Eventuous.Azure.ServiceBus.Producers;

public class ServiceBusProducer : BaseProducer<ServiceBusProduceOptions>, IHostedProducer, IAsyncDisposable
{
    // maybe want something a bit more focused on Azure Service Bus?
    static readonly ProducerTracingOptions TracingOptions = new() { MessagingSystem = "azure-service-bus", DestinationKind = "topic", ProduceOperation = "publish" };
    private readonly ServiceBusProducerOptions options;
    private readonly ILogger<ServiceBusProducer>? log;
    private readonly ServiceBusSender sender;
    private readonly IEventSerializer serializer;
    private readonly ServiceBusMessageBatchBuilder messageBatchBuilder;

    public ServiceBusProducer(
        ServiceBusClient client,
        ServiceBusProducerOptions options,
        IEventSerializer? serializer = null,
        ILogger<ServiceBusProducer>? log = null) : base(TracingOptions)
    {
        this.options = options;
        this.log = log;
        this.sender = client.CreateSender(options.QueueOrTopicName, options.SenderOptions);
        this.serializer = serializer ?? DefaultEventSerializer.Instance;
        this.messageBatchBuilder = new ServiceBusMessageBatchBuilder(sender, this.serializer, options.Attributes, SetActivityMessageType);
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
        if (messages is ProducedMessage[] singleMessage && singleMessage.Length == 1)
        {
            await ProcessSingleMessage(stream, options, singleMessage, cancellationToken);
        }
        else
        {
            await ProcessMessagesInBatches(stream, messages, options, cancellationToken);
        }
    }

    private async Task ProcessSingleMessage(StreamName stream, ServiceBusProduceOptions? options, ProducedMessage[] singleMessage, CancellationToken cancellationToken)
    {
        var message = singleMessage[0];
        var serviceBusMessage = new ServiceBusMessageBuilder(
            serializer,
            stream,
            this.options.Attributes,
            options,
            SetActivityMessageType
        ).CreateServiceBusMessage(message);

        log?.LogInformation("Sending single message to {QueueOrTopicName}", this.options.QueueOrTopicName);
        try
        {
            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
            await message.Ack<ServiceBusProducer>();
            log?.LogInformation("Single message sent successfully to {QueueOrTopicName}", this.options.QueueOrTopicName);
        }
        catch (Exception ex)
        {
            log?.LogError(ex, "Failed to send single message to {QueueOrTopicName}", this.options.QueueOrTopicName);
            await message.Nack<ServiceBusProducer>("Failed to send single message", ex);
        }
        return;
    }

    private async Task ProcessMessagesInBatches(StreamName stream, IEnumerable<ProducedMessage> messages, ServiceBusProduceOptions? options, CancellationToken cancellationToken)
    {
        await foreach (var (batch, produced) in messageBatchBuilder.CreateMessageBatches(messages, stream, options, cancellationToken))
        {
            log?.LogInformation("Sending batch of {MessageCount} messages to {QueueOrTopicName}", batch.Count, this.options.QueueOrTopicName);
            try
            {
                await sender.SendMessagesAsync(batch, cancellationToken);
                foreach (var message in produced)
                {
                    await message.Ack<ServiceBusProducer>();
                }
                log?.LogInformation("Batch sent successfully to {QueueOrTopicName}", this.options.QueueOrTopicName);
            }
            catch (Exception ex)
            {
                log?.LogError(ex, "Failed to send batch to {QueueOrTopicName}", this.options.QueueOrTopicName);
                foreach (var message in produced)
                {
                    await message.Nack<ServiceBusProducer>("Failed to send batch", ex);
                }
            }
        }
    }

    public async Task Produce1(StreamName stream, IEnumerable<ProducedMessage> messages, ServiceBusProduceOptions? options, CancellationToken cancellationToken = default)
    {
        await ProduceMessages(stream, messages, options, cancellationToken);
    }
}
