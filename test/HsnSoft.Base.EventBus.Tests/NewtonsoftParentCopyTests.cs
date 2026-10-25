using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HsnSoft.Base.EventBus.Tests;

/// <summary>
/// The services in production keep the message they are handling as a parent by copying it through Newtonsoft
/// (<c>JsonConvert.DeserializeObject&lt;ParentMessageEnvelope&gt;(JsonConvert.SerializeObject(envelope))</c>) and pass that copy to
/// <c>PublishAsync(parentMessage: ...)</c>. That must keep working.
/// </summary>
public class NewtonsoftParentCopyTests
{
    private static MessageEnvelope<SampleEto> HandledMessage()
    {
        MessageEnvelope<SampleEto> envelope = new()
        {
            MessageId = Guid.CreateVersion7(),
            HopLevel = 2,
            CorrelationId = "corr",
            Producer = "HHS_Other",
            Message = new SampleEto("m", 1),
        };
        envelope.SetHeader("TraceUser", "user-7");
        envelope.SetHeader("ClientChannel", "web");
        return envelope;
    }

    private static ParentMessageEnvelope CopyLikeProduction(MessageEnvelope<SampleEto> envelope)
        => JsonConvert.DeserializeObject<ParentMessageEnvelope>(JsonConvert.SerializeObject(envelope))!;

    private static EventEnvelopeFactory NewFactory() => new(Options.Create(new EventBusOptions()), []);

    [Fact]
    public void CopiedParent_KeepsTheChain_AndCanBePublishedFrom()
    {
        MessageEnvelope<SampleEto> handled = HandledMessage();
        ParentMessageEnvelope copy = CopyLikeProduction(handled);

        MessageEnvelope<SampleEto> child = NewFactory().Create(new SampleEto("c", 2), copy, null, false);
        byte[] written = EventSerializer.Serialize(child);

        MessageEnvelope<SampleEto> read = (MessageEnvelope<SampleEto>)EventSerializer.DeserializeEnvelope(written, typeof(SampleEto));
        Assert.Equal(handled.MessageId, read.ParentMessageId);
        Assert.Equal((ushort)3, read.HopLevel);
        Assert.Equal("corr", read.CorrelationId);
    }

    [Fact]
    public void CopiedParent_OfTheMessageBeingHandled_StillPassesTheHeadersOn()
    {
        MessageEnvelope<SampleEto> handled = HandledMessage();
        ParentMessageEnvelope copy = CopyLikeProduction(handled);

        MessageEnvelope<SampleEto> child;
        using (EventContext.Use(handled))
        {
            child = NewFactory().Create(new SampleEto("c", 2), copy, null, false);
        }

        Assert.Equal("user-7", child.GetHeader("TraceUser"));
        Assert.Equal("web", child.GetHeader("ClientChannel"));
    }

    [Fact]
    public void CopiedParent_OfAnotherMessage_DoesNotTakeTheHeadersOfTheOneBeingHandled()
    {
        MessageEnvelope<SampleEto> handled = HandledMessage();
        ParentMessageEnvelope someoneElses = CopyLikeProduction(HandledMessage());

        MessageEnvelope<SampleEto> child;
        using (EventContext.Use(handled))
        {
            child = NewFactory().Create(new SampleEto("c", 2), someoneElses, null, false);
        }

        Assert.Equal(someoneElses.MessageId, child.ParentMessageId);
        Assert.Null(child.GetHeader("TraceUser"));
    }

    [Fact]
    public void HeadersTheParentHasItself_WinOverTheOnesOfTheMessageBeingHandled()
    {
        MessageEnvelope<SampleEto> handled = HandledMessage();
        ParentMessageEnvelope parent = new() { MessageId = handled.MessageId, HopLevel = 2 };
        parent.SetHeader("TraceUser", "from-the-parent");

        MessageEnvelope<SampleEto> child;
        using (EventContext.Use(handled))
        {
            child = NewFactory().Create(new SampleEto("c", 2), parent, null, false);
        }

        Assert.Equal("from-the-parent", child.GetHeader("TraceUser"));
        Assert.Equal("web", child.GetHeader("ClientChannel"));
    }
}
