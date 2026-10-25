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
            told.Add,
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
            told.Add,
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
            _ => stopping.Cancel(),
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
            _ => told++,
            stopping.Token);

        Assert.Equal(0, told);
    }
}
