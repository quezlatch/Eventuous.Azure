using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Filters;
using Microsoft.Extensions.Logging;

namespace Eventuous.Azure.ServiceBus.Subscriptions;

public class ServiceBusSubscription : EventSubscription<ServiceBusSubscriptionOptions>
{
    public ServiceBusSubscription(ServiceBusSubscriptionOptions options, ConsumePipe consumePipe, ILoggerFactory? loggerFactory, IEventSerializer? eventSerializer) : base(options, consumePipe, loggerFactory, eventSerializer)
    {
    }

    protected override ValueTask Subscribe(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override ValueTask Unsubscribe(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
