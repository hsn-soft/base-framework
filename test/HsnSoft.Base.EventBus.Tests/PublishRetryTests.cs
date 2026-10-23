namespace HsnSoft.Base.EventBus.Tests;

public class PublishRetryTests
{
    private static readonly TimeSpan s_tiny = TimeSpan.FromMilliseconds(1);

    [Fact]
    public async Task SucceedingAction_RunsOnce()
    {
        int calls = 0;

        await PublishRetry.ExecuteAsync(() => { calls++; return Task.CompletedTask; }, 5, s_tiny, _ => true, (_, _, _) => { });

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task TransientFailure_IsRetriedUntilItSucceeds_WithGrowingPauses()
    {
        int calls = 0;
        List<(TimeSpan Delay, int Retry)> retries = [];

        await PublishRetry.ExecuteAsync(
            () => ++calls < 3 ? throw new InvalidOperationException("down") : Task.CompletedTask,
            5,
            TimeSpan.FromMilliseconds(2),
            _ => true,
            (_, delay, retry) => retries.Add((delay, retry)));

        Assert.Equal(3, calls);
        Assert.Equal([(TimeSpan.FromMilliseconds(4), 1), (TimeSpan.FromMilliseconds(8), 2)], retries);
    }

    [Fact]
    public async Task TransientFailure_ThatNeverGoesAway_IsThrownAfterTheRetries()
    {
        int calls = 0;

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => PublishRetry.ExecuteAsync(
            () => { calls++; throw new InvalidOperationException("down"); },
            2,
            s_tiny,
            _ => true,
            (_, _, _) => { }));

        Assert.Equal("down", failure.Message);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task FailureThatIsNotTransient_IsThrownAtOnce()
    {
        int calls = 0;

        await Assert.ThrowsAsync<ArgumentException>(() => PublishRetry.ExecuteAsync(
            () => { calls++; throw new ArgumentException("bad"); },
            5,
            s_tiny,
            exception => exception is InvalidOperationException,
            (_, _, _) => { }));

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Cancellation_IsNeverRetried()
    {
        int calls = 0;

        await Assert.ThrowsAsync<OperationCanceledException>(() => PublishRetry.ExecuteAsync(
            () => { calls++; throw new OperationCanceledException(); },
            5,
            s_tiny,
            _ => true,
            (_, _, _) => { }));

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CancellingTheWait_StopsTheRetries()
    {
        using CancellationTokenSource cancellation = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => PublishRetry.ExecuteAsync(
            () => throw new InvalidOperationException("down"),
            5,
            TimeSpan.FromMinutes(1),
            _ => true,
            (_, _, _) => cancellation.Cancel(),
            cancellation.Token));
    }

    [Fact]
    public async Task NullArguments_AreRejected()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => PublishRetry.ExecuteAsync(null!, 1, s_tiny, _ => true, (_, _, _) => { }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => PublishRetry.ExecuteAsync(() => Task.CompletedTask, 1, s_tiny, null!, (_, _, _) => { }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => PublishRetry.ExecuteAsync(() => Task.CompletedTask, 1, s_tiny, _ => true, null!));
    }
}
