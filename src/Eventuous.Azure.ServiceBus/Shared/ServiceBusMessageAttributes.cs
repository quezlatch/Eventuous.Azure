namespace Eventuous.Azure.ServiceBus.Shared;

public class ServiceBusMessageAttributes
{
    public string EventType { get; set; } = "eventType";
    public string StreamName { get; set; } = "streamName";
}