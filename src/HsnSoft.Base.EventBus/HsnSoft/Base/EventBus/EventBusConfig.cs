namespace HsnSoft.Base.EventBus;

public class EventBusConfig
{
    public string EventBusConnectionString { get; set; } = string.Empty;

    public string SubscriberClientAppName { get; set; } = "HsnSoft_event_bus_client";
    public string DefaultTopicName { get; set; } = "HsnSoft_event_bus";
    public int ConnectionRetryCount { get; set; } = 5;

    public string EventNamePrefix { get; set; } = string.Empty;

    public string EventNameSuffix { get; set; } = string.Empty;

    public object Connection { get; set; }

    public bool DeleteEventPrefix => !string.IsNullOrEmpty(EventNamePrefix);
    public bool DeleteEventSuffix => !string.IsNullOrEmpty(EventNameSuffix);
}