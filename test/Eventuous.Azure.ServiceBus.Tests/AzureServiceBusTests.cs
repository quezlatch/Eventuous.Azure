using Eventuous.Azure.ServiceBus.Producers;
using Testcontainers.ServiceBus;
using Testcontainers.Xunit;

namespace Eventuous.Azure.ServiceBus.Tests;

public class AzureServiceBusTests : IClassFixture<AzureServiceBusFixture>
{
    readonly AzureServiceBusFixture _fixture;
    private readonly ServiceBusProducer producer;

    public AzureServiceBusTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
        producer = _fixture.CreateProducer(new ServiceBusProducerOptions
        {
            QueueOrTopicName = "test-topic"
        });
    }

    [Fact]
    public void TestServiceBusConnection()
    {
        var container = _fixture.Container;
        Assert.NotNull(container);
        Assert.Equal("", container.GetConnectionString());
    }
}

