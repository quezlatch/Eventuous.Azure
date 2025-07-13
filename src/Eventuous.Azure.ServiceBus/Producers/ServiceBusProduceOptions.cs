namespace Eventuous.Azure.ServiceBus.Producers;

/// <summary>
/// Options for producing messages to Azure Service Bus.
/// We can have some fun with this in the future, like adding retry policies, dead-lettering, delayed delivery, transactional support, etc.
/// This is a placeholder for now.
/// </summary>
public class ServiceBusProduceOptions
{
    public string? Subject { get; set; }
    public string? To { get; set; }
    public string? ReplyTo { get; set; }
    public TimeSpan TimeToLive { get; set; }
}