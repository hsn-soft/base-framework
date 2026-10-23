using Serilog.Debugging;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog.Tests;

[Collection(ProcessWideCollection.Name)]
public class RefreshableSinkTests
{
    [Fact]
    public void Events_GoToTheCurrentSink()
    {
        DisposableSink first = new();
        RefreshableSink sink = new(() => first);

        sink.Emit(Events.Create(LogEventLevel.Information));

        Assert.Single(first.Events);
    }

    [Fact]
    public void Refresh_SwapsInANewSink_AndDisposesTheOldOne()
    {
        List<DisposableSink> built = [];
        RefreshableSink sink = new(
            () =>
            {
                DisposableSink next = new();
                built.Add(next);
                return next;
            },
            TimeSpan.Zero);

        sink.Emit(Events.Create(LogEventLevel.Information));
        sink.Refresh();
        sink.Emit(Events.Create(LogEventLevel.Warning));

        Assert.Equal(2, built.Count);
        Assert.True(built[0].Disposed);
        Assert.False(built[1].Disposed);
        Assert.Single(built[0].Events);
        Assert.Single(built[1].Events);
    }

    [Fact]
    public async Task ReplacedSink_LivesOnForTheRetireDelay_SoWhatItSendsIsNotLost()
    {
        List<DisposableSink> built = [];
        RefreshableSink sink = new(
            () =>
            {
                DisposableSink next = new();
                built.Add(next);
                return next;
            },
            TimeSpan.FromMilliseconds(300));

        sink.Refresh();
        Assert.False(built[0].Disposed);

        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (!built[0].Disposed && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.True(built[0].Disposed);
        Assert.False(built[1].Disposed);
    }

    [Fact]
    public void ReplacedSinkThatCannotBeDisposed_IsJustLetGo()
    {
        RefreshableSink sink = new(() => new CaptureSink(), TimeSpan.Zero);

        sink.Refresh();

        sink.Emit(Events.Create(LogEventLevel.Information));
    }

    [Fact]
    public void Refresh_WhenTheNewSinkCannotBeBuilt_KeepsTheOldOne_AndSaysSo()
    {
        List<string> selfLog = [];
        SelfLog.Enable(selfLog.Add);
        try
        {
            DisposableSink old = new();
            int calls = 0;
            RefreshableSink sink = new(() => calls++ == 0 ? old : throw new InvalidOperationException("graylog is not reachable"), TimeSpan.Zero);

            sink.Refresh();
            sink.Emit(Events.Create(LogEventLevel.Information));

            Assert.False(old.Disposed);
            Assert.Single(old.Events);
            Assert.Contains(selfLog, line => line.Contains("graylog is not reachable", StringComparison.Ordinal));
        }
        finally
        {
            SelfLog.Disable();
        }
    }

    [Fact]
    public void Sink_ThatCannotBeBuiltAtFirst_IsAnError()
    {
        Assert.Throws<InvalidOperationException>(() => new RefreshableSink(() => throw new InvalidOperationException("no address")));
    }

    [Fact]
    public void Dispose_DisposesTheCurrentSink_IfItCanBe()
    {
        DisposableSink inner = new();
        new RefreshableSink(() => inner).Dispose();
        new RefreshableSink(() => new CaptureSink()).Dispose();

        Assert.True(inner.Disposed);
    }

    [Fact]
    public async Task RefreshService_RebuildsTheSinkOnASchedule_NotAtStart()
    {
        int built = 0;
        RefreshableSink sink = new(() =>
        {
            Interlocked.Increment(ref built);
            return new CaptureSink();
        });
        GraylogSinkRefreshService service = new(sink, TimeSpan.FromMilliseconds(40), Microsoft.Extensions.Logging.Abstractions.NullLogger<GraylogSinkRefreshService>.Instance);

        await service.StartAsync(CancellationToken.None);
        Assert.Equal(1, Volatile.Read(ref built));
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (Volatile.Read(ref built) < 3 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        await service.StopAsync(CancellationToken.None);
        Assert.True(Volatile.Read(ref built) >= 3, "the sink was not rebuilt on the schedule");
    }
}
