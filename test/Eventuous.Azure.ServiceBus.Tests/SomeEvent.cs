namespace Eventuous.Azure.ServiceBus.Tests;

[EventType("V1.SomeEvent")]
public class SomeEvent
{
    static SomeEvent()
    {
        TypeMap.RegisterKnownEventTypes(typeof(SomeEvent).Assembly);
    }
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Some Event";
}
