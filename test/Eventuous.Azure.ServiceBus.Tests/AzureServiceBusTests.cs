using Azure.Messaging.ServiceBus;
using Eventuous.Azure.ServiceBus.Producers;
using Eventuous.Azure.ServiceBus.Subscriptions;
using Eventuous.Producers;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Context;

namespace Eventuous.Azure.ServiceBus.Tests;

public class AzureServiceBusTests : IClassFixture<AzureServiceBusFixture>, IAsyncLifetime
{
    readonly AzureServiceBusFixture _fixture;
    private readonly ServiceBusProducer producer;
    private readonly ServiceBusSubscription subscription;
    private readonly TestEventHandler handler = new();
    private readonly StreamName topic = new($"test-{Guid.NewGuid():N}");

    public AzureServiceBusTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
        producer = _fixture.CreateProducer(new ServiceBusProducerOptions
        {
            QueueOrTopicName = AzureServiceBusFixture.TopicName,
        });
        subscription = _fixture.CreateSubscription(new ServiceBusSubscriptionOptions
        {
            QueueOrTopicName = AzureServiceBusFixture.TopicName,
            SubscriptionId = AzureServiceBusFixture.SubscriptionName
        }, handler);
    }

    public async ValueTask DisposeAsync()
    {
        await producer.StopAsync(TestContext.Current.CancellationToken);
        await subscription.Unsubscribe(id => { }, TestContext.Current.CancellationToken);
    }

    public async ValueTask InitializeAsync()
    {
        await producer.StartAsync(TestContext.Current.CancellationToken);
        await subscription.Subscribe(id => { }, (id, reason, ex) => { }, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SubscribeAndProduce()
    {
        var evt = new SomeEvent { Id = "test-event", Name = "Hello, World!" };
        await producer.Produce(topic, evt, null, cancellationToken: TestContext.Current.CancellationToken);
    }

    private class TestEventHandler : BaseEventHandler
    {
        public override ValueTask<EventHandlingStatus> HandleEvent(IMessageConsumeContext context)
        {
            throw new NotImplementedException();
        }
    }
}

