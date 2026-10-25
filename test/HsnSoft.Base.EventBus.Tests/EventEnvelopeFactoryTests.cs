using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Tests;

public class EventEnvelopeFactoryTests
{
    private static EventEnvelopeFactory NewFactory(params IEventHeaderProvider[] providers)
        => new(Options.Create(new EventBusOptions { ExchangeName = "HHS", ConsumerClientName = "OrderService" }), providers);

    private sealed class FakeUser(Guid? id, Guid? tenantId, bool isSystemTenant = false, params string[] roles) : HsnSoft.Base.Security.ICurrentUser
    {
        public bool IsAuthenticated => id.HasValue;

        public Guid? Id => id;

        public string? UserName => id.HasValue ? "alice" : null;

        public Guid? TenantId => tenantId;

        public bool IsSystemTenant => isSystemTenant;

        public IReadOnlyList<Guid> AllowedTenantIds => [];

        public IReadOnlyList<string> Roles => roles;
    }

    private static EventEnvelopeFactory NewFactory(HsnSoft.Base.Security.ICurrentUser user)
        => new(Options.Create(new EventBusOptions { ExchangeName = "HHS", ConsumerClientName = "OrderService" }), [], user);

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
        EventEnvelopeFactory factory = NewFactory(new FixedHeaderProvider(null, ("TraceUser", "provided"), ("ClientChannel", "web")));
        ParentMessageEnvelope parent = new();
        parent.SetHeader("TraceUser", "from-parent");
        parent.SetHeader("ClientVersion", "2.0");

        MessageEnvelope<SampleEto> envelope = factory.Create(s_message, parent, null, false);

        Assert.Equal("from-parent", envelope.GetHeader("TraceUser"));
        Assert.Equal("web", envelope.GetHeader("ClientChannel"));
        Assert.Equal("2.0", envelope.GetHeader("ClientVersion"));
    }

    [Fact]
    public void ParentsMessage_IsNotInheritedAsAHeader()
    {
        ParentMessageEnvelope parent = new();
        parent.SetHeader("Message", "{}");
        parent.SetHeader("TraceUser", "u");

        MessageEnvelope<SampleEto> envelope = NewFactory().Create(s_message, parent, null, false);

        Assert.Null(envelope.GetHeader("Message"));
        Assert.Equal("u", envelope.GetHeader("TraceUser"));
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

    [Fact]
    public void TheCallerOfTheEnvelope_IsWhoeverIsActingNow()
    {
        Guid user = Guid.NewGuid();
        Guid tenant = Guid.NewGuid();

        MessageEnvelope<SampleEto> envelope = NewFactory(new FakeUser(user, tenant, true, "admin", "customer")).Create(s_message, null, null, isReQueuePublish: false);

        Assert.Equal((user, "alice", tenant, true), (envelope.UserId, envelope.UserName, envelope.TenantId, envelope.IsSystemTenant));
        Assert.Equal(["admin", "customer"], envelope.UserRoles);
    }

    [Fact]
    public void WhenNobodyActs_TheEnvelopeHasNoCaller_AndNoRolesList()
    {
        MessageEnvelope<SampleEto> envelope = NewFactory(new FakeUser(null, null)).Create(s_message, null, null, isReQueuePublish: false);

        Assert.False(envelope.IsSystemTenant);
        Assert.Null(envelope.UserId);
        Assert.Null(envelope.TenantId);
        Assert.Null(envelope.UserRoles);
        Assert.Null(NewFactory().Create(s_message, null, null, isReQueuePublish: false).TenantId);
    }

    [Fact]
    public void AParentsCaller_WinsOverWhoIsActing()
    {
        Guid parentUser = Guid.NewGuid();
        Guid parentTenant = Guid.NewGuid();
        ParentMessageEnvelope parent = new() { UserId = parentUser, UserName = "bob", TenantId = parentTenant, UserRoles = ["support"] };

        MessageEnvelope<SampleEto> envelope = NewFactory(new FakeUser(Guid.NewGuid(), Guid.NewGuid())).Create(s_message, parent, null, isReQueuePublish: false);

        Assert.Equal((parentUser, "bob", parentTenant), (envelope.UserId, envelope.UserName, envelope.TenantId));
        Assert.Equal(["support"], envelope.UserRoles);
    }

    [Fact]
    public void AParentWithoutACaller_LeavesItToWhoIsActing()
    {
        Guid user = Guid.NewGuid();

        MessageEnvelope<SampleEto> envelope = NewFactory(new FakeUser(user, null)).Create(s_message, new ParentMessageEnvelope(), null, isReQueuePublish: false);

        Assert.Equal(user, envelope.UserId);
    }
}
