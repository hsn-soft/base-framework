using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus.Tests;

public class EventContextTests
{
    [Fact]
    public void Outside_ThereIsNoCurrentMessage()
    {
        Assert.Null(EventContext.Current);
    }

    [Fact]
    public void UsedEnvelope_IsCurrentUntilDisposed_AndNestedScopesRestoreTheOuterOne()
    {
        ParentMessageEnvelope outer = new() { HopLevel = 1 };
        ParentMessageEnvelope inner = new() { HopLevel = 2 };

        using (EventContext.Use(outer))
        {
            Assert.Same(outer, EventContext.Current);
            using (EventContext.Use(inner))
            {
                Assert.Same(inner, EventContext.Current);
            }

            Assert.Same(outer, EventContext.Current);
        }

        Assert.Null(EventContext.Current);
    }

    [Fact]
    public async Task CurrentMessage_FollowsAsyncCalls()
    {
        ParentMessageEnvelope envelope = new() { HopLevel = 7 };

        using (EventContext.Use(envelope))
        {
            await Task.Delay(10);
            ParentMessageEnvelope? seen = await Task.Run(() => EventContext.Current);

            Assert.Same(envelope, seen);
        }
    }

    [Fact]
    public void NullEnvelope_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => EventContext.Use(null!));
    }
}
