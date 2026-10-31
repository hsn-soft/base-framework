using NLog.Targets.Wrappers;

namespace HsnSoft.Base.Logging.NLog.Tests;

public sealed class GelfHttpTargetSettingsTests
{
    [Fact]
    public void TheConnectionToGraylog_IsMadeAgainOften_SoAMovedGraylogIsFound_AndAMissingOneIsGivenUpFast()
    {
        using System.Net.Http.SocketsHttpHandler handler = GelfHttpTarget.CreateHandler();

        Assert.Equal(TimeSpan.FromMinutes(2), handler.PooledConnectionLifetime);
        Assert.Equal(TimeSpan.FromSeconds(3), handler.ConnectTimeout);
    }

    [Fact]
    public void TheQueue_IsBounded_AndDropsTheLogsThatDoNotFit_NotTheMemoryOfTheApplication()
    {
        using GelfHttpTarget target = new("graylog.internal", 12201);

        Assert.Equal(GelfHttpTarget.MaxQueuedLogs, target.QueueLimit);
        Assert.Equal(10_000, GelfHttpTarget.MaxQueuedLogs);
        Assert.Equal(AsyncTargetWrapperOverflowAction.Discard, target.OverflowAction);
    }
}
