using Azure.Messaging.ServiceBus;
using Eventuous.Azure.ServiceBus.Producers;

namespace Eventuous.Azure.ServiceBus.Tests;

public class ConvertEventToMessage
{
    private readonly Guid messageId = Guid.NewGuid();
    private readonly ServiceBusMessage message;

    public ConvertEventToMessage()
    {
        var builder = new ServiceBusMessageBuilder(DefaultEventSerializer.Instance, "test-stream", new ServiceBusProduceOptions
        {
            Subject = "test-subject",
            To = "test-to",
            ReplyTo = "test-reply-to",
            TimeToLive = TimeSpan.FromMinutes(5)
        });
        message = builder.CreateServiceBusMessage(new Eventuous.Producers.ProducedMessage(
            new SomeEvent
            {
                Id = "event-id",
                Name = "Test Event"
            },
            new Metadata
            {
                [MetaTags.MessageId] = "12345",
                [MetaTags.CorrelationId] = "correlation-id",
                [MetaTags.CausationId] = "causation-id",
                ["AAA"] = 1111
            },
            new Metadata
            {
                ["BBB"] = "12345",
            },
            messageId
        ));
    }
    [Fact]
    public void ContentType()
    {
        Assert.Equal("application/json", message.ContentType);
    }

    [Fact]
    public void MessageId()
    {
        Assert.Equal(messageId.ToString(), message.MessageId);
    }

    [Fact]
    public void Subject()
    {
        Assert.Equal("test-subject", message.Subject);
    }

    [Fact]
    public void To()
    {
        Assert.Equal("test-to", message.To);
    }

    [Fact]
    public void ReplyTo()
    {
        Assert.Equal("test-reply-to", message.ReplyTo);
    }

    [Fact]
    public void TimeToLive()
    {
        Assert.Equal(TimeSpan.FromMinutes(5), message.TimeToLive);
    }

    [Fact]
    public void CorrelationId()
    {
        Assert.Equal("correlation-id", message.CorrelationId);
    }

    [Theory]
    [InlineData("MessageId")]
    [InlineData("CorrelationId")]
    public void ApplicationPropertiesHasNoReservedAttributes(string propertyName)
    {
        Assert.False(message.ApplicationProperties.ContainsKey(propertyName));
    }

    [Theory]
    [InlineData(MetaTags.CausationId, "causation-id")]
    [InlineData("AAA", 1111)]
    [InlineData("BBB", "12345")]
    public void ApplicationPropertiesHasAttributes(string propertyName, object expectedValue)
    {
        Assert.Equal(expectedValue, message.ApplicationProperties[propertyName]);
    }
}