using System.Text;
using System.Text.Json;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus.Tests;

/// <summary>The wire format must stay the one the services in production write and read: flat, PascalCase.</summary>
public class EnvelopeSerializationTests
{
    // what the production framework writes (MessageEnvelope serialized with default System.Text.Json options)
    private const string ProductionJson = """
        {
          "HopLevel": 2,
          "ReQueuedCount": 1,
          "MessageId": "0190a2f4-8c1e-7b3a-9d5e-2f6a4c8e1b07",
          "MessageTime": "2026-05-01T08:30:00Z",
          "CorrelationId": "corr-1",
          "TraceUser": "user-7",
          "Region": "admin,editor",
          "ClientLat": "41.01",
          "ClientLong": null,
          "ClientChannel": "web",
          "ClientVersion": "3.2.1",
          "Producer": "HHS_IdentityService",
          "ParentMessageId": "0190a2f4-8c1e-7b3a-9d5e-2f6a4c8e1b06",
          "Message": { "Name": "keyboard", "Count": 3 }
        }
        """;

    private static byte[] Bytes(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void ProductionMessage_IsReadIntoTheEnvelopeAndTheMessage()
    {
        object read = EventSerializer.DeserializeEnvelope(Bytes(ProductionJson), typeof(SampleEto));

        MessageEnvelope<SampleEto> envelope = Assert.IsType<MessageEnvelope<SampleEto>>(read);
        Assert.Equal((ushort)2, envelope.HopLevel);
        Assert.Equal((ushort)1, envelope.ReQueuedCount);
        Assert.Equal(Guid.Parse("0190a2f4-8c1e-7b3a-9d5e-2f6a4c8e1b07"), envelope.MessageId);
        Assert.Equal(Guid.Parse("0190a2f4-8c1e-7b3a-9d5e-2f6a4c8e1b06"), envelope.ParentMessageId);
        Assert.Equal(new DateTime(2026, 5, 1, 8, 30, 0, DateTimeKind.Utc), envelope.MessageTime.ToUniversalTime());
        Assert.Equal("corr-1", envelope.CorrelationId);
        Assert.Equal("HHS_IdentityService", envelope.Producer);
        Assert.Equal(new SampleEto("keyboard", 3), envelope.Message);
    }

    [Fact]
    public void ProductSpecificProperties_AreKeptAsHeaders()
    {
        MessageEnvelope<SampleEto> envelope = (MessageEnvelope<SampleEto>)EventSerializer.DeserializeEnvelope(Bytes(ProductionJson), typeof(SampleEto));

        Assert.Equal("user-7", envelope.GetHeader("TraceUser"));
        Assert.Equal("admin,editor", envelope.GetHeader("Region"));
        Assert.Equal("41.01", envelope.GetHeader("ClientLat"));
        Assert.Null(envelope.GetHeader("ClientLong"));
        Assert.Equal("web", envelope.GetHeader("ClientChannel"));
        Assert.Equal("3.2.1", envelope.GetHeader("ClientVersion"));
    }

    [Fact]
    public void WrittenMessage_IsFlatAndPascalCase_LikeProductionWritesIt()
    {
        MessageEnvelope<SampleEto> envelope = new()
        {
            HopLevel = 1,
            MessageId = Guid.CreateVersion7(),
            MessageTime = DateTime.UtcNow,
            CorrelationId = "corr-2",
            Producer = "HHS_OrderService",
            Message = new SampleEto("mouse", 1),
        };
        envelope.SetHeader("TraceUser", "user-9");

        using JsonDocument written = JsonDocument.Parse(EventSerializer.Serialize(envelope));

        JsonElement root = written.RootElement;
        Assert.Equal(1, root.GetProperty("HopLevel").GetInt32());
        Assert.Equal("corr-2", root.GetProperty("CorrelationId").GetString());
        Assert.Equal("HHS_OrderService", root.GetProperty("Producer").GetString());
        Assert.Equal("user-9", root.GetProperty("TraceUser").GetString());
        Assert.Equal("mouse", root.GetProperty("Message").GetProperty("Name").GetString());
        Assert.Equal(1, root.GetProperty("Message").GetProperty("Count").GetInt32());
        Assert.False(root.TryGetProperty("Headers", out _));
        Assert.False(root.TryGetProperty("hopLevel", out _));
    }

    [Fact]
    public void WrittenMessage_RoundTripsThroughTheReader()
    {
        MessageEnvelope<SampleEto> envelope = new()
        {
            HopLevel = 3,
            ReQueuedCount = 2,
            MessageId = Guid.CreateVersion7(),
            ParentMessageId = Guid.CreateVersion7(),
            MessageTime = DateTime.UtcNow,
            CorrelationId = "corr-3",
            Producer = "p",
            Message = new SampleEto("a", 2),
        };
        envelope.SetHeader("ClientChannel", "ios");

        MessageEnvelope<SampleEto> read = (MessageEnvelope<SampleEto>)EventSerializer.DeserializeEnvelope(EventSerializer.Serialize(envelope), typeof(SampleEto));

        Assert.Equal(envelope.MessageId, read.MessageId);
        Assert.Equal(envelope.ParentMessageId, read.ParentMessageId);
        Assert.Equal(envelope.HopLevel, read.HopLevel);
        Assert.Equal(envelope.ReQueuedCount, read.ReQueuedCount);
        Assert.Equal(envelope.Message, read.Message);
        Assert.Equal("ios", read.GetHeader("ClientChannel"));
    }

    [Fact]
    public void Names_AreReadWithoutRegardToCase()
    {
        string camelCase = """{"hopLevel":4,"messageId":"0190a2f4-8c1e-7b3a-9d5e-2f6a4c8e1b07","message":{"name":"x","count":9}}""";

        MessageEnvelope<SampleEto> read = (MessageEnvelope<SampleEto>)EventSerializer.DeserializeEnvelope(Bytes(camelCase), typeof(SampleEto));

        Assert.Equal((ushort)4, read.HopLevel);
        Assert.Equal(new SampleEto("x", 9), read.Message);
    }

    [Fact]
    public void ParentIsReadFromAnyMessage_WithoutTheMessageAsHeader()
    {
        ParentMessageEnvelope parent = EventSerializer.DeserializeParent(Bytes(ProductionJson));

        Assert.Equal("corr-1", parent.CorrelationId);
        Assert.Equal("user-7", parent.GetHeader("TraceUser"));
        Assert.Null(parent.GetHeader("Message"));
        Assert.Null(parent.GetHeader("ParentMessageId"));
        Assert.DoesNotContain(parent.Headers!.Keys, key => ParentMessageEnvelope.IsEnvelopeProperty(key));
    }

    [Fact]
    public void ParentOfAMessageWithoutExtraProperties_HasNoHeaders()
    {
        ParentMessageEnvelope parent = EventSerializer.DeserializeParent(Bytes("""{"HopLevel":1}"""));

        Assert.Equal((ushort)1, parent.HopLevel);
        Assert.Null(parent.Headers);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("not json")]
    public void UnusableBody_IsAJsonException(string body)
    {
        Assert.ThrowsAny<JsonException>(() => EventSerializer.DeserializeEnvelope(Bytes(body), typeof(SampleEto)));
        Assert.ThrowsAny<JsonException>(() => EventSerializer.DeserializeParent(Bytes(body)));
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => EventSerializer.Serialize<SampleEto>(null!));
        Assert.Throws<ArgumentNullException>(() => EventSerializer.DeserializeEnvelope(Bytes("{}"), null!));
    }

    [Fact]
    public void FailedEto_KeepsItsShapeOnTheWire()
    {
        FailedEto failed = new("boom", new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), new SampleEto("n", 1), "SampleEto");
        MessageEnvelope<FailedEto> envelope = new() { MessageId = Guid.CreateVersion7(), Message = failed };

        using JsonDocument written = JsonDocument.Parse(EventSerializer.Serialize(envelope));
        MessageEnvelope<FailedEto> read = (MessageEnvelope<FailedEto>)EventSerializer.DeserializeEnvelope(EventSerializer.Serialize(envelope), typeof(FailedEto));

        JsonElement message = written.RootElement.GetProperty("Message");
        Assert.Equal("boom", message.GetProperty("FailedReason").GetString());
        Assert.Equal("SampleEto", message.GetProperty("FailedMessageTypeName").GetString());
        Assert.Equal("n", message.GetProperty("FailedMessageObject").GetProperty("Name").GetString());
        Assert.Equal("boom", read.Message.FailedReason);
        Assert.Equal("SampleEto", read.Message.FailedMessageTypeName);
    }

    [Fact]
    public void ReQueuedEto_KeepsItsShapeOnTheWire()
    {
        ReQueuedEto reQueued = new("HHS_OrderService", new SampleEto("n", 1), "SampleEto");
        MessageEnvelope<ReQueuedEto> envelope = new() { MessageId = Guid.CreateVersion7(), Message = reQueued };

        MessageEnvelope<ReQueuedEto> read = (MessageEnvelope<ReQueuedEto>)EventSerializer.DeserializeEnvelope(EventSerializer.Serialize(envelope), typeof(ReQueuedEto));

        Assert.Equal("HHS_OrderService", read.Message.ReQueuedMessageEnvelopeConsumer);
        Assert.Equal("SampleEto", read.Message.ReQueuedMessageTypeName);
        Assert.IsType<JsonElement>(read.Message.ReQueuedMessageObject);
    }

    [Fact]
    public void MessageCausedByAnother_IsWrittenWithOneParentMessageId()
    {
        // the parent was read from JSON that already names its own parent, as every message in a chain does
        ParentMessageEnvelope parent = EventSerializer.DeserializeParent(Bytes(ProductionJson));
        MessageEnvelope<SampleEto> child = new EventEnvelopeFactory(
            Microsoft.Extensions.Options.Options.Create(new EventBusOptions()),
            []).Create(new SampleEto("c", 1), parent, null, false);

        string written = Encoding.UTF8.GetString(EventSerializer.Serialize(child));
        MessageEnvelope<SampleEto> read = (MessageEnvelope<SampleEto>)EventSerializer.DeserializeEnvelope(Bytes(written), typeof(SampleEto));

        Assert.Equal(1, System.Text.RegularExpressions.Regex.Count(written, "\"ParentMessageId\""));
        Assert.Equal(parent.MessageId, read.ParentMessageId);
    }
}
