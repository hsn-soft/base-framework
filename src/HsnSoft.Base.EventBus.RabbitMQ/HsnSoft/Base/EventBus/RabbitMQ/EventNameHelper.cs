namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>The names of the queues. They are what the services in production already use, so they must not change.</summary>
internal static class EventNameHelper
{
    /// <summary>The queue a service reads one event from: <c>{ExchangeName}_{ConsumerClientName}_{EventName}</c>.</summary>
    internal static string GetConsumerClientEventQueueName(EventBusOptions options, string eventName)
        => $"{options.ConsumerClientInfo}_{EventNames.Trim(options, eventName)}";

    /// <summary>The queue another service reads one event from (a message sent to it): <c>{ExchangeName}_{service}_{EventName}</c>.</summary>
    internal static string GetConsumerClientEventQueueName(EventBusOptions options, string service, string eventName)
        => $"{(string.IsNullOrWhiteSpace(options.ExchangeName) ? string.Empty : options.ExchangeName + "_")}{service}_{EventNames.Trim(options, eventName)}";

    /// <summary>The queue a requeued message goes to: <c>{consumer}_{EventName}</c>, the consumer being <c>{ExchangeName}_{ConsumerClientName}</c>.</summary>
    internal static string GetConsumerReQueuedEventQueueName(string reQueuedConsumer, string eventName)
        => $"{reQueuedConsumer}_{eventName}";

    /// <summary>The queue failed messages of the service go to: <c>{ExchangeName}_{ErrorClientName}_{EventName}</c>.</summary>
    internal static string GetErrorEventQueueName(EventBusOptions options, string eventName)
        => $"{options.ErrorClientInfo}_{eventName}";
}
