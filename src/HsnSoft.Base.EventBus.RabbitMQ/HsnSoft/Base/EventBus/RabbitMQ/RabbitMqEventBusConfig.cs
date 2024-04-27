namespace HsnSoft.Base.EventBus.RabbitMQ;

public class RabbitMqEventBusConfig : EventBusConfig
{
    public ushort ConsumerMaxFetchCount { get; set; } = 5;
}