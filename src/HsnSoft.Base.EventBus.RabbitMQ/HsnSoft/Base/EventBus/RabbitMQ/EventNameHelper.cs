using HsnSoft.Base.EventBus.RabbitMQ.Configs;

namespace HsnSoft.Base.EventBus.RabbitMQ;

internal static class EventNameHelper
{
    internal static string GetConsumerClientEventQueueName(RabbitMqEventBusConfig rabbitMqEventBusConfig, string eventName)
    {
        return $"{rabbitMqEventBusConfig.ConsumerClientInfo}_{TrimEventName(rabbitMqEventBusConfig, eventName)}";
    }

    internal static string TrimEventName(RabbitMqEventBusConfig rabbitMqEventBusConfig, string eventName)
    {
        if (rabbitMqEventBusConfig.DeleteEventPrefix && eventName.StartsWith(rabbitMqEventBusConfig.EventNamePrefix))
        {
            eventName = eventName[rabbitMqEventBusConfig.EventNamePrefix.Length..];
        }

        if (rabbitMqEventBusConfig.DeleteEventSuffix && eventName.EndsWith(rabbitMqEventBusConfig.EventNameSuffix))
        {
            eventName = eventName[..^rabbitMqEventBusConfig.EventNameSuffix.Length];
        }

        return eventName;
    }
}