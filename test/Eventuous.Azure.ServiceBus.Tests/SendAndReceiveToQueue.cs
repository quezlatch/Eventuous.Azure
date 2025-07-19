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
        var evt = new SomeEvent { Id = "test-event", Name = "Hello, World!" };
        await producer.Produce(new(_fixture.QueueName), evt, null, cancellationToken: TestCancellationToken);

        // Assert
        await handler.AssertThat()
            .Timebox(TimeSpan.FromSeconds(10))
            .Single()
            .Match(evt => evt is SomeEvent)
            .Validate(TestCancellationToken);
    }

    [Fact]
    public async Task LoadsOfMessages()
    {
        var count = 1000;
        var events = Enumerable.Range(0, count).Select(i => new SomeEvent { Id = $"test-event-{i}", Name = $"Hello, World! {i}" }).ToList();
        await producer.Produce(new(_fixture.QueueName), events, null, cancellationToken: TestCancellationToken);

        // Assert
        await handler.AssertThat()
            .Timebox(TimeSpan.FromSeconds(30))
            .Exactly(count)
            .Match(evt => evt is SomeEvent)
            .Validate(TestCancellationToken);

        var zipped = events.Zip(handler.Messages.Cast<SomeEvent>(), (sent, received) => (sent, received));
        foreach (var (sent, received) in zipped)
        {
            Assert.Equal(sent.Id, received.Id);
            Assert.Equal(sent.Name, received.Name);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await producer.StopAsync(TestCancellationToken);
        await subscription.Unsubscribe(id => { }, TestCancellationToken);
        await subscription.DisposeAsync();
        await producer.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await producer.StartAsync(TestCancellationToken);
        await subscription.Subscribe(id => { }, (id, reason, ex) => { }, TestCancellationToken);
    }
}
