using Eventuous.Azure.ServiceBus.Producers;
using Eventuous.Azure.ServiceBus.Subscriptions;
using Eventuous.Producers;

namespace Eventuous.Azure.ServiceBus.Tests;

public abstract class SendAndReceive : IAsyncLifetime
{
    public const string QueueName = "queue.1";
    public const string TopicName = "topic.1";
    /// <summary>
    /// This is strange. The 'subscription.1' in the emulator has a content type filter. we populate
    /// the content type but it still gets filtered out. So we use 'subscription.3' which has no filters.
    /// </summary>
    public const string SubscriptionName = "subscription.3";
    public static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;
    private readonly ServiceBusProducer producer;
    private readonly ServiceBusSubscription subscription;
    private readonly string correlationId;
    private readonly Metadata metadata;
    private readonly TestEventHandler handler = new();

    protected abstract ServiceBusProducerOptions ServiceBusProducerOptions { get; }
    protected abstract ServiceBusSubscriptionOptions ServiceBusSubscriptionOptions { get; }
    protected abstract StreamName StreamName { get; }

    public SendAndReceive(AzureServiceBusFixture fixture)
    {
        producer = fixture.CreateProducer(ServiceBusProducerOptions);
        correlationId = Guid.NewGuid().ToString();
        subscription = fixture.CreateSubscription(ServiceBusSubscriptionOptions, handler, correlationId);
        metadata = new Metadata().With(MetaTags.CorrelationId, correlationId);
    }

    [Collection(nameof(AzureServiceBusFixture))]
    public class ToQueue : SendAndReceive
    {
        public ToQueue(AzureServiceBusFixture fixture) : base(fixture) { }

        protected override ServiceBusProducerOptions ServiceBusProducerOptions => new()
        {
            QueueOrTopicName = QueueName
        };

        protected override ServiceBusSubscriptionOptions ServiceBusSubscriptionOptions => new()
        {
            QueueOrTopic = new Queue(QueueName),
            SubscriptionId = SubscriptionName
        };

        protected override StreamName StreamName => new(QueueName);
    }

    [Collection(nameof(AzureServiceBusFixture))]
    public class ToTopic : SendAndReceive
    {
        public ToTopic(AzureServiceBusFixture fixture) : base(fixture) { }

        protected override ServiceBusProducerOptions ServiceBusProducerOptions => new()
        {
            QueueOrTopicName = TopicName
        };

        protected override ServiceBusSubscriptionOptions ServiceBusSubscriptionOptions => new()
        {
            QueueOrTopic = new Topic(TopicName),
            SubscriptionId = SubscriptionName
        };

        protected override StreamName StreamName => new(TopicName);
    }

    [Collection(nameof(AzureServiceBusFixture))]
    public class ToTopicWithSubscription : SendAndReceive
    {
        public ToTopicWithSubscription(AzureServiceBusFixture fixture) : base(fixture) { }

        protected override ServiceBusProducerOptions ServiceBusProducerOptions => new()
        {
            QueueOrTopicName = TopicName,
        };

        protected override ServiceBusSubscriptionOptions ServiceBusSubscriptionOptions => new()
        {
            QueueOrTopic = new TopicAndSubscription(TopicName, SubscriptionName),
            SubscriptionId = "some-subscription" // Use a different subscription name to avoid conflicts
        };

        protected override StreamName StreamName => new(TopicName);
    }

    [Fact]
    public async Task SingleMessage()
    {
        await producer.Produce(StreamName, SomeEvent.Create(), metadata, cancellationToken: TestCancellationToken);

        // Assert
        await handler.AssertThat()
            .Timebox(TimeSpan.FromSeconds(1))
            .Single()
            .Match(evt => evt is SomeEvent)
            .Validate(TestCancellationToken);
    }

    [Fact]
    public async Task LoadsOfMessages()
    {
        var count = 200;
        var events = Enumerable.Range(0, count).Select(SomeEvent.Create).ToList();
        await producer.Produce(StreamName, events, metadata, cancellationToken: TestCancellationToken);

        // Assert
        await handler.AssertThat()
            .Timebox(TimeSpan.FromSeconds(10))
            .Exactly(count)
            .Match(evt => evt is SomeEvent)
            .Validate(TestCancellationToken);

        var handledMessages = handler.Messages
            .OfType<SomeEvent>()
            .OrderBy(m => m.Id)
            .ToList();
        Assert.Equal(events, handledMessages, (x, y) => x.Id == y.Id);
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
