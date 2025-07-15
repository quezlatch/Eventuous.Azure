using Azure.Messaging.ServiceBus;
using DotNet.Testcontainers.Builders;
using Eventuous.Azure.ServiceBus.Producers;
using Eventuous.Azure.ServiceBus.Subscriptions;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Context;
using Eventuous.Subscriptions.Filters;
using Eventuous.Subscriptions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.ServiceBus;
using Testcontainers.Xunit;
using Xunit.Sdk;

namespace Eventuous.Azure.ServiceBus.Tests;

public class AzureServiceBusFixture(IMessageSink messageSink)
: ContainerFixture<ServiceBusBuilder, ServiceBusContainer>(messageSink)
{
    public ServiceBusClient Client { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;

    private readonly TestEventHandler handler = new TestEventHandler();

    protected override ServiceBusBuilder Configure(ServiceBusBuilder builder) =>
        builder
            .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
    .WithAcceptLicenseAgreement(true)
    .WithExposedPort(5671)
    .WithExposedPort(8080)
    .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5671));


    protected override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        ConnectionString = Container.GetConnectionString();
        Client = new ServiceBusClient(ConnectionString);
    }

    public ServiceBusProducer CreateProducer(ServiceBusProducerOptions options) =>
        new ServiceBusProducer(
            Client,
            options,
            serializer: null,
            log: NullLogger<ServiceBusProducer>.Instance
        );

    public ServiceBusSubscription CreateSubscription(
        ServiceBusSubscriptionOptions options
    ) => new ServiceBusSubscription(
        Client,
        options,
                    new ConsumePipe().AddDefaultConsumer(handler),
                    null,
                    null
    );

    private class TestEventHandler : BaseEventHandler
    {
        public override ValueTask<EventHandlingStatus> HandleEvent(IMessageConsumeContext context)
        {
            throw new NotImplementedException();
        }
    }
}
