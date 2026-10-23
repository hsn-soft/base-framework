namespace HsnSoft.Base.EventBus.RabbitMQ.Tests;

/// <summary>The names of the queues are what the services in production use; a change would strand their messages.</summary>
public class EventNameHelperTests
{
    private static EventBusOptions Options() => new() { ExchangeName = "HHS", ConsumerClientName = "OrderService", ErrorClientName = "EventManagerService" };

    [Fact]
    public void QueueOfAnEvent_IsTheExchangeTheConsumerAndTheEvent()
    {
        Assert.Equal("HHS_OrderService_Sample", EventNameHelper.GetConsumerClientEventQueueName(Options(), "Sample"));
    }

    [Fact]
    public void QueueOfAnEvent_WorksWithATypeNameToo()
    {
        Assert.Equal("HHS_OrderService_Sample", EventNameHelper.GetConsumerClientEventQueueName(Options(), "SampleEto"));
    }

    [Fact]
    public void QueueOfARequeue_IsTheConsumerAndTheEvent()
    {
        Assert.Equal("HHS_PaymentService_ReQueued", EventNameHelper.GetConsumerReQueuedEventQueueName("HHS_PaymentService", "ReQueued"));
    }

    [Fact]
    public void QueueOfFailedMessages_IsTheErrorClientsAndTheEvent()
    {
        Assert.Equal("HHS_EventManagerService_Failed", EventNameHelper.GetErrorEventQueueName(Options(), "Failed"));
    }
}
