using System.Text;
using System.Text.Json;

namespace HsnSoft.Base.EventBus.Tests;

public class EventSerializerTests
{
    [Fact]
    public void RoundTrip_KeepsEveryValue()
    {
        var original = new SampleEvent("order", 3);

        var restored = Assert.IsType<SampleEvent>(EventSerializer.Deserialize(EventSerializer.Serialize(original), typeof(SampleEvent)));

        Assert.Equal(original, restored);
        Assert.Equal(original.EventId, restored.EventId);
        Assert.Equal(original.OccurredAt, restored.OccurredAt);
    }

    [Fact]
    public void Serialize_UsesCamelCase()
        => Assert.Contains("\"name\":\"order\"", Encoding.UTF8.GetString(EventSerializer.Serialize(new SampleEvent("order", 1))));

    [Fact]
    public void Deserialize_NullBody_Throws()
        => Assert.Throws<JsonException>(() => EventSerializer.Deserialize("null"u8, typeof(SampleEvent)));

    [Fact]
    public void Deserialize_InvalidJson_Throws()
        => Assert.ThrowsAny<JsonException>(() => EventSerializer.Deserialize("{ not json"u8, typeof(SampleEvent)));

    [Fact]
    public void IntegrationEvent_GetsAnIdAndATimestamp()
    {
        var created = new SampleEvent("x", 1);

        Assert.NotEqual(Guid.Empty, created.EventId);
        Assert.InRange(created.OccurredAt, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.NotEqual(created.EventId, new SampleEvent("x", 1).EventId);
    }
}
