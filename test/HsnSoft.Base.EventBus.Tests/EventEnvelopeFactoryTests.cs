using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Tests;

public class EventEnvelopeFactoryTests
{
    private static EventEnvelopeFactory NewFactory(params IEventHeaderProvider[] providers)
        => new(Options.Create(new EventBusOptions { ExchangeName = "HHS", ConsumerClientName = "OrderService" }), providers);

    private static readonly SampleEto s_message = new("m", 1);

    [Fact]
    public void MessageNobodyCaused_StartsAChain()
    {
        MessageEnvelope<SampleEto> envelope = NewFactory().Create(s_message, null, null, isReQueuePublish: false);

        Assert.Equal((ushort)1, envelope.HopLevel);
        Assert.Equal((ushort)0, envelope.ReQueuedCount);
        Assert.Null(envelope.ParentMessageId);
        Assert.NotEqual(Guid.Empty, envelope.MessageId);
        Assert.Equal("OrderService", envelope.Producer);
        Assert.Same(s_message, envelope.Message);
        Assert.True((DateTime.UtcNow - envelope.MessageTime).Duration() < TimeSpan.FromMinutes(1));
        Assert.Null(envelope.CorrelationId);
        // the only header of a message from nowhere: it says it comes from an agent
        Assert.Equal("agent", envelope.GetHeader(ParentMessageEnvelope.FlowHeader));
        Assert.Single(envelope.Headers!);
    }

    [Fact]
    public void MessagePublishedWhileAStepRuns_ComesFromThatStep_AndTakesItsCorrelationId()
    {
        EventEnvelopeFactory factory = NewFactory(new FixedHeaderProvider("from-provider"));
        ParentMessageEnvelope parent = new() { CorrelationId = null };
        parent.SetHeader(ParentMessageEnvelope.FlowHeader, "token-login");

        using (LogFlow.Begin("flow-corr", "scope", "ordercreated"))
        {
            MessageEnvelope<SampleEto> fromFlow = factory.Create(s_message, null, null, false);
            MessageEnvelope<SampleEto> fromParent = factory.Create(s_message, parent, null, false);

            Assert.Equal("flow-corr", fromFlow.CorrelationId);
            Assert.Equal("ordercreated", fromFlow.GetHeader(ParentMessageEnvelope.FlowHeader));
            // a parent's own flow is not inherited: this message comes from what is running now
            Assert.Equal("ordercreated", fromParent.GetHeader(ParentMessageEnvelope.FlowHeader));
            Assert.Equal("flow-corr", fromParent.CorrelationId);
        }
    }

    [Fact]
    public void MessageIds_AreUnique()
    {
        EventEnvelopeFactory factory = NewFactory();

        Assert.NotEqual(
            factory.Create(s_message, null, null, false).MessageId,
            factory.Create(s_message, null, null, false).MessageId);
    }

    [Fact]
    public void CorrelationId_ComesFromTheCaller_ThenTheParent_ThenTheProviders()
    {
        EventEnvelopeFactory factory = NewFactory(new FixedHeaderProvider(null), new FixedHeaderProvider("from-provider"));
        ParentMessageEnvelope parent = new() { CorrelationId = "from-parent" };

        Assert.Equal("from-caller", factory.Create(s_message, parent, "from-caller", false).CorrelationId);
        Assert.Equal("from-parent", factory.Create(s_message, parent, null, false).CorrelationId);
        Assert.Equal("from-provider", factory.Create(s_message, null, null, false).CorrelationId);
    }

    [Fact]
    public void MessageCausedByAnother_ContinuesItsChain()
    {
        ParentMessageEnvelope parent = new() { MessageId = Guid.CreateVersion7(), HopLevel = 2, ReQueuedCount = 1 };

        MessageEnvelope<SampleEto> envelope = NewFactory().Create(s_message, parent, null, isReQueuePublish: false);

        Assert.Equal(parent.MessageId, envelope.ParentMessageId);
        Assert.Equal((ushort)3, envelope.HopLevel);
        Assert.Equal((ushort)1, envelope.ReQueuedCount);
    }

    [Fact]
    public void ReQueuedMessage_CountsTheReQueue()
    {
        ParentMessageEnvelope parent = new() { ReQueuedCount = 1 };

        Assert.Equal((ushort)2, NewFactory().Create(s_message, parent, null, isReQueuePublish: true).ReQueuedCount);
        Assert.Equal((ushort)1, NewFactory().Create(s_message, null, null, isReQueuePublish: true).ReQueuedCount);
    }

    [Fact]
    public void ProvidedHeaders_AreAdded_AndTheParentsWin()
    {
        EventEnvelopeFactory factory = NewFactory(new FixedHeaderProvider(null, ("UserId", "provided"), ("ClientChannel", "web")));
        ParentMessageEnvelope parent = new();
        parent.SetHeader("UserId", "from-parent");
        parent.SetHeader("ClientVersion", "2.0");

        MessageEnvelope<SampleEto> envelope = factory.Create(s_message, parent, null, false);

        Assert.Equal("from-parent", envelope.GetHeader("UserId"));
        Assert.Equal("web", envelope.GetHeader("ClientChannel"));
        Assert.Equal("2.0", envelope.GetHeader("ClientVersion"));
    }

    [Fact]
    public void ParentsMessage_IsNotInheritedAsAHeader()
    {
        ParentMessageEnvelope parent = new();
        parent.SetHeader("Message", "{}");
        parent.SetHeader("UserId", "u");

        MessageEnvelope<SampleEto> envelope = NewFactory().Create(s_message, parent, null, false);

        Assert.Null(envelope.GetHeader("Message"));
        Assert.Equal("u", envelope.GetHeader("UserId"));
    }

    [Fact]
    public void MessageBeingHandled_IsTheParentUnlessTheCallerSaysOtherwise()
    {
        ParentMessageEnvelope handling = new() { MessageId = Guid.CreateVersion7(), HopLevel = 4, CorrelationId = "ctx" };
        ParentMessageEnvelope explicitParent = new() { MessageId = Guid.CreateVersion7(), HopLevel = 1 };
        EventEnvelopeFactory factory = NewFactory();

        using (EventContext.Use(handling))
        {
            MessageEnvelope<SampleEto> followed = factory.Create(s_message, null, null, false);
            MessageEnvelope<SampleEto> overridden = factory.Create(s_message, explicitParent, null, false);

            Assert.Equal(handling.MessageId, followed.ParentMessageId);
            Assert.Equal((ushort)5, followed.HopLevel);
            Assert.Equal("ctx", followed.CorrelationId);
            Assert.Equal(explicitParent.MessageId, overridden.ParentMessageId);
        }

        Assert.Null(factory.Create(s_message, null, null, false).ParentMessageId);
    }

    [Fact]
    public void NullMessage_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => NewFactory().Create<SampleEto>(null!, null, null, false));
    }
}
