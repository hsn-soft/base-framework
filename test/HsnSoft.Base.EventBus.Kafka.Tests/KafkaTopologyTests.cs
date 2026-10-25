using Confluent.Kafka;

namespace HsnSoft.Base.EventBus.Kafka.Tests;

public class KafkaTopologyTests
{
    [Theory]
    [InlineData("XYZ", "OrderService", "OrderCreated", "XYZ_OrderService_OrderCreated")]
    [InlineData("XYZ", "ShipmentService", "OrderCreatedEto", "XYZ_ShipmentService_OrderCreated")]
    [InlineData("", "OrderService", "OrderCreated", "OrderService_OrderCreated")]
    public void TheGroupOfAService_IsNamedLikeItsRabbitMqQueueForTheEvent(string exchange, string service, string eventName, string expected)
    {
        EventBusOptions options = new() { ExchangeName = exchange, ConsumerClientName = service };

        Assert.Equal(expected, KafkaTopology.GroupFor(options, eventName));
    }

    [Fact]
    public void TwoServices_HaveGroupsOfTheirOwnForTheSameEvent()
    {
        EventBusOptions order = new() { ExchangeName = "XYZ", ConsumerClientName = "OrderService" };
        EventBusOptions product = new() { ExchangeName = "XYZ", ConsumerClientName = "ProductService" };

        Assert.NotEqual(KafkaTopology.GroupFor(order, "OrderCreated"), KafkaTopology.GroupFor(product, "OrderCreated"));
    }

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
