using Eventuous.Azure.ServiceBus.Producers;
using Eventuous.Azure.ServiceBus.Subscriptions;
using Eventuous.Producers;

namespace Eventuous.Azure.ServiceBus.Tests;

public class SendAndReceiveToQueue : IClassFixture<AzureServiceBusFixture>, IAsyncLifetime
{
    public static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;
    private readonly AzureServiceBusFixture _fixture;
    private readonly ServiceBusProducer producer;
    private readonly ServiceBusSubscription subscription;
    private readonly TestEventHandler handler = new();

    public SendAndReceiveToQueue(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
        producer = _fixture.CreateProducer(new ServiceBusProducerOptions
        {
            QueueOrTopicName = _fixture.QueueName,
        });
        subscription = _fixture.CreateSubscription(new ServiceBusSubscriptionOptions
        {
            QueueOrTopicName = _fixture.QueueName,
            SubscriptionId = _fixture.SubscriptionName
        }, handler);
    }

    [Fact]
    public async Task SingleMessage()
    {
        // Arrange
        var client = _fixture.Client;

        var evt = new SomeEvent { Id = "test-event", Name = "Hello, World!" };
        await producer.Produce(new(_fixture.QueueName), evt, null, cancellationToken: TestCancellationToken);

        // Assert
        await handler.AssertThat()
            .Timebox(TimeSpan.FromSeconds(10))
            .Any()
            .Match(evt => evt is SomeEvent)
            .Validate(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await producer.StopAsync(TestContext.Current.CancellationToken);
        await subscription.Unsubscribe(id => { }, TestCancellationToken);
        await subscription.DisposeAsync();
        await producer.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await producer.StartAsync(TestContext.Current.CancellationToken);
        await subscription.Subscribe(id => { }, (id, reason, ex) => { }, TestCancellationToken);
    }
}
