namespace HsnSoft.Base.EventBus.RabbitMQ.Configs;

public class RabbitMqEventBusConfig : EventBusConfig
{
    public ushort ChannelParallelThreadCount { get; set; } = 1;
}