using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Context;
using Hypothesist;
using Hypothesist.Builders;

namespace Eventuous.Azure.ServiceBus.Tests;

public class TestEventHandler(TimeSpan? delay = null) : BaseEventHandler
{
    readonly TimeSpan _delay = delay ?? TimeSpan.Zero;

    public int Count { get; private set; }
    readonly Observer<object> _observer = new();

    public On<object> AssertThat() => Hypothesis.On(_observer);

    public override async ValueTask<EventHandlingStatus> HandleEvent(IMessageConsumeContext context)
    {
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"Handling event {context.MessageType} with id {context.MessageId} in {context.Stream}");
        await Task.Delay(_delay);
        await _observer.Add(context.Message!, context.CancellationToken);
        Count++;

        return EventHandlingStatus.Success;
    }
}