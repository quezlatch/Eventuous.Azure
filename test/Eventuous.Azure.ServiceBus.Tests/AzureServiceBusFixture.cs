using Azure.Messaging.ServiceBus;
using Eventuous.Azure.ServiceBus.Producers;
using Eventuous.Azure.ServiceBus.Subscriptions;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Filters;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.ServiceBus;
using Testcontainers.Xunit;
using Xunit.Sdk;

namespace Eventuous.Azure.ServiceBus.Tests;

[CollectionDefinition(nameof(AzureServiceBusFixture), DisableParallelization = true)]
public class AzureServiceBusCollection : ICollectionFixture<AzureServiceBusFixture>
{
    // This class is used to define a collection fixture for the Azure Service Bus tests.
    // It ensures that the tests in this collection are run sequentially and share the same fixture instance.
}

public class AzureServiceBusFixture(IMessageSink messageSink)
    : ContainerFixture<ServiceBusBuilder, ServiceBusContainer>(messageSink)
{
    public ServiceBusClient Client { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;

    protected override ServiceBusBuilder Configure(ServiceBusBuilder builder) =>
        builder
            .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
            .WithAcceptLicenseAgreement(true);

    protected override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        ConnectionString = Container.GetConnectionString();

        Client = new ServiceBusClient(ConnectionString, new ServiceBusClientOptions
        {
            TransportType = ServiceBusTransportType.AmqpTcp
        });
    }


    public ServiceBusProducer CreateProducer(ServiceBusProducerOptions options) =>
        new(
            Client,
            options,
            serializer: null,
            log: NullLogger<ServiceBusProducer>.Instance
        );

    public ServiceBusSubscription CreateSubscription(
        ServiceBusSubscriptionOptions options,
        IEventHandler handler,
        string correlationId) => new(
            Client,
            options,
            new ConsumePipe()
                .AddFilterFirst(FilterOnCorrelationId(correlationId))
                .AddDefaultConsumer(handler),
            null,
            null
        );

    /// <summary>
    /// So that we can use the same service bus subscription for multiple tests
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    private static MessageFilter FilterOnCorrelationId(string correlationId) =>
        new(message =>
            message.Metadata?[MetaTags.CorrelationId]?.ToString() == correlationId);
}
