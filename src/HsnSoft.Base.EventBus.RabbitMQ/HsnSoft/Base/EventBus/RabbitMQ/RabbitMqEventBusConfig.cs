namespace HsnSoft.Base.EventBus.RabbitMQ;

public class RabbitMqEventBusConfig : EventBusConfig
{
    public ushort ConsumerMaxFetchCount { get; set; } = 1;
    public ushort ConsumerParallelThreadCount { get; set; } = 1;
}