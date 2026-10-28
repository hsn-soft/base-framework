using Confluent.Kafka;

namespace HsnSoft.Base.EventBus.Kafka.Tests;

public class KafkaRecoveryTests
{
    [Fact]
    public async Task AConsumerThatFails_IsToldAndStartedAgain_UntilItEndsOnItsOwn()
    {
        int runs = 0;
        List<Exception> told = [];

        await KafkaRecovery.RunAsync(
            _ => ++runs < 3 ? throw new KafkaException(new Error(ErrorCode.Local_Transport)) : Task.CompletedTask,
            TimeSpan.Zero,
            TimeSpan.Zero,
            (exception, _) => told.Add(exception),
            CancellationToken.None);

        Assert.Equal(3, runs);
        Assert.Equal(2, told.Count);
        Assert.All(told, exception => Assert.IsType<KafkaException>(exception));
    }

    [Fact]
    public async Task AnyFailure_IsToldAndRetried_ACancellationThatIsNotTheStopToo()
    {
        int runs = 0;
        List<Exception> told = [];

        await KafkaRecovery.RunAsync(
            _ =>
            {
                runs++;
                return runs switch
                {
                    1 => throw new InvalidOperationException("unexpected"),
                    2 => throw new OperationCanceledException(),
                    _ => Task.CompletedTask,
                };
            },
            TimeSpan.Zero,
            TimeSpan.Zero,
            (exception, _) => told.Add(exception),
            CancellationToken.None);

        Assert.Equal(3, runs);
        Assert.IsType<InvalidOperationException>(told[0]);
        Assert.IsType<OperationCanceledException>(told[1]);
    }

    [Fact]
    public async Task WhenTheServiceStops_TheLoopEnds_WhetherItIsRunningOrWaiting()
    {
        using CancellationTokenSource stopping = new();
        int runs = 0;

        Task loop = KafkaRecovery.RunAsync(
            _ =>
            {
                runs++;
                throw new KafkaException(new Error(ErrorCode.Local_Transport));
            },
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5),
            (_, _) => stopping.Cancel(),
            stopping.Token);

        await loop.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task ACancellationOfTheStop_EndsTheLoopWithoutTellingAnybody()
    {
        using CancellationTokenSource stopping = new();
        int told = 0;

        await KafkaRecovery.RunAsync(
            cancellationToken =>
            {
                stopping.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            },
            TimeSpan.Zero,
            TimeSpan.Zero,
            (_, _) => told++,
            stopping.Token);

        Assert.Equal(0, told);
    }

    [Fact]
    public async Task TheWaitDoublesWithEveryFailureInARow_UpToTheMaximum()
    {
        int runs = 0;
        List<TimeSpan> waits = [];

        await KafkaRecovery.RunAsync(
            _ => ++runs < 6 ? throw new KafkaException(new Error(ErrorCode.Local_Transport)) : Task.CompletedTask,
            TimeSpan.FromMilliseconds(5),
            TimeSpan.FromMilliseconds(20),
            (_, wait) => waits.Add(wait),
            CancellationToken.None);

        Assert.Equal([5, 10, 20, 20, 20], waits.Select(wait => (int)wait.TotalMilliseconds));
    }

    [Fact]
    public async Task ARunThatLastedLongerThanTheMaximum_StartsTheWaitOverAgain()
    {
        int runs = 0;
        List<TimeSpan> waits = [];

        await KafkaRecovery.RunAsync(
            async _ =>
            {
                runs++;
                if (runs == 3)
                {
                    await Task.Delay(80);
                }

                if (runs < 4)
                {
                    throw new KafkaException(new Error(ErrorCode.Local_Transport));
                }
            },
            TimeSpan.FromMilliseconds(5),
            TimeSpan.FromMilliseconds(40),
            (_, wait) => waits.Add(wait),
            CancellationToken.None);

        // the first two failures wait 5 and 10 ms; the third came after a long healthy run, so its wait is the first one again
        Assert.Equal([5, 10, 5], waits.Select(wait => (int)wait.TotalMilliseconds));
    }
}
