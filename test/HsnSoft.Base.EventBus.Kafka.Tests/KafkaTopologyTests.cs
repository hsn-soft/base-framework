using Confluent.Kafka;

namespace HsnSoft.Base.EventBus.Kafka.Tests;

public class KafkaTopologyTests
{
    [Fact]
    public async Task ABrokerThatAnswersLate_IsTriedAgain()
    {
        int calls = 0;

        await KafkaTopology.RetryAsync(
            () => ++calls < 3 ? throw new KafkaException(new Error(ErrorCode.Local_Transport)) : Task.CompletedTask,
            attempts: 5,
            TimeSpan.Zero);

        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task ABrokerThatNeverAnswers_IsGivenUpOn_WithItsError()
    {
        int calls = 0;

        KafkaException error = await Assert.ThrowsAsync<KafkaException>(() => KafkaTopology.RetryAsync(
            () =>
            {
                calls++;
                throw new KafkaException(new Error(ErrorCode.Local_Transport));
            },
            attempts: 3,
            TimeSpan.Zero));

        Assert.Equal(ErrorCode.Local_Transport, error.Error.Code);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task AnythingThatIsNotKafkaFailing_IsNotRetried()
    {
        int calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => KafkaTopology.RetryAsync(
            () =>
            {
                calls++;
                throw new InvalidOperationException();
            },
            attempts: 3,
            TimeSpan.Zero));

        Assert.Equal(1, calls);
    }
}
