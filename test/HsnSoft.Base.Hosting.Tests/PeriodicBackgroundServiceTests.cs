using Microsoft.Extensions.Logging.Abstractions;

namespace HsnSoft.Base.Hosting.Tests;

public class PeriodicBackgroundServiceTests
{
    private static readonly TimeSpan Fast = TimeSpan.FromMilliseconds(10);

    private sealed class CountingWorker(TimeSpan interval, Func<int, CancellationToken, Task>? work = null)
        : PeriodicBackgroundService(interval, NullLogger.Instance)
    {
        private int _runs;

        public int Runs => Volatile.Read(ref _runs);

        protected override Task RunOnceAsync(CancellationToken cancellationToken)
            => work?.Invoke(Interlocked.Increment(ref _runs), cancellationToken) ?? CompleteAfterCounting();

        private Task CompleteAfterCounting()
        {
            Interlocked.Increment(ref _runs);
            return Task.CompletedTask;
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The condition was not met in time.");
            await Task.Delay(5);
        }
    }

    [Fact]
    public async Task Runs_Immediately_And_Then_Repeatedly()
    {
        var worker = new CountingWorker(Fast);

        await worker.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => worker.Runs >= 3);
        await worker.StopAsync(CancellationToken.None);

        Assert.True(worker.Runs >= 3);
    }

    [Fact]
    public async Task First_Run_Does_Not_Wait_For_The_Interval()
    {
        var worker = new CountingWorker(TimeSpan.FromHours(1));

        await worker.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => worker.Runs == 1);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(1, worker.Runs);
    }

    [Fact]
    public async Task A_Failed_Run_Is_Logged_And_The_Next_Run_Still_Happens()
    {
        var worker = new CountingWorker(Fast, (run, _) => run == 1 ? throw new InvalidOperationException("boom") : Task.CompletedTask);

        await worker.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => worker.Runs >= 3);
        await worker.StopAsync(CancellationToken.None);

        Assert.True(worker.Runs >= 3);
    }

    [Fact]
    public async Task Stopping_Cancels_A_Run_That_Is_In_Progress()
    {
        var started = new TaskCompletionSource();
        var worker = new CountingWorker(Fast, async (_, token) =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
        });

        await worker.StartAsync(CancellationToken.None);
        await started.Task;
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(1, worker.Runs);
    }

    [Fact]
    public async Task Stopping_While_Waiting_For_The_Next_Tick_Ends_The_Worker()
    {
        var worker = new CountingWorker(TimeSpan.FromHours(1));

        await worker.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => worker.Runs == 1);
        await worker.StopAsync(CancellationToken.None);

        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public void The_Interval_Must_Be_Positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CountingWorker(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CountingWorker(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void The_Logger_Is_Required()
        => Assert.Throws<ArgumentNullException>(() => new NullLoggerWorker());

    private sealed class NullLoggerWorker() : PeriodicBackgroundService(Fast, null!)
    {
        protected override Task RunOnceAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
