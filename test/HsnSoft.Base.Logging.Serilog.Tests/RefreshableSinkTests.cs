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

        Assert.Null(Record.Exception(() => sink.Emit(Events.Create(LogEventLevel.Information))));
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
    public void Dispose_AlsoDisposesTheReplacedSinksStillWaitingOutTheirDelay_SoTheirBatchesAreSent()
    {
        List<DisposableSink> built = [];
        RefreshableSink sink = new(
            () =>
            {
                DisposableSink next = new();
                built.Add(next);
                return next;
            },
            TimeSpan.FromHours(1));

        sink.Refresh();
        sink.Refresh();
        Assert.False(built[0].Disposed);
        Assert.False(built[1].Disposed);

        // the application stops: nobody is left to wait for the hour
        sink.Dispose();

        Assert.All(built, one => Assert.True(one.Disposed));
    }

    [Fact]
    public async Task ASinkThatWasDisposedWithTheRest_IsNotDisposedAgainWhenItsDelayEnds()
    {
        List<DisposableSink> built = [];
        RefreshableSink sink = new(
            () =>
            {
                DisposableSink next = new();
                built.Add(next);
                return next;
            },
            TimeSpan.FromMilliseconds(150));

        sink.Refresh();
        sink.Dispose();
        await Task.Delay(400);

        Assert.All(built, one => Assert.Equal(1, one.Disposals));
    }

    [Fact]
    public void AfterDispose_NothingIsBuiltAndDisposeAgainChangesNothing()
    {
        int built = 0;
        RefreshableSink sink = new(() =>
        {
            built++;
            return new DisposableSink();
        });

        sink.Dispose();
        sink.Refresh();
        sink.Dispose();

        Assert.Equal(1, built);
    }

    [Fact]
    public void ASinkReplacedWhileTheApplicationStops_IsDisposedAtOnce()
    {
        // the swap lost the race with Dispose: what the old sink holds is sent now, not left for a timer that will not fire
        DisposableSink first = new();
        DisposableSink second = new();
        int calls = 0;
        RefreshableSink sink = new(() => calls++ == 0 ? first : second, TimeSpan.FromHours(1));

        sink.Refresh();
        sink.Dispose();

        Assert.True(first.Disposed);
        Assert.True(second.Disposed);
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
