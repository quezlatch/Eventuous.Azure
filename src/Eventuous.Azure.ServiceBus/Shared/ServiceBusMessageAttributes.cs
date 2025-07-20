namespace Eventuous.Azure.ServiceBus.Shared;

public class ServiceBusMessageAttributes
{
    public string EventType { get; set; } = "eventType";
    public string StreamName { get; set; } = "streamName";
    public string CorrelationId { get; set; } = MetaTags.CorrelationId;
    public string CausationId { get; set; } = MetaTags.CausationId;
    public string ReplyTo { get; set; } = "replyTo";
    public string Subject { get; set; } = "subject";
    public string To { get; set; } = "to";
    public string MessageId { get; set; } = "messageId";
}