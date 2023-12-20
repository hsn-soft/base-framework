namespace HsnSoft.Base.EventBus;

public class EventBusConfig
{
    public string ExchangeName { get; set; } = "HsnSoft";
    public string ClientName { get; set; } = "Client";

    public string EventNamePrefix { get; set; } = "";
    public string EventNameSuffix { get; set; } = "IntegrationEvent";

    public bool DeleteEventPrefix => !string.IsNullOrEmpty(EventNamePrefix);

    public bool DeleteEventSuffix => !string.IsNullOrEmpty(EventNameSuffix);

    public string ClientInfo =>
        (string.IsNullOrWhiteSpace(ExchangeName) ? string.Empty : $"{ExchangeName}_") +
        (string.IsNullOrWhiteSpace(ClientName) ? string.Empty : $"{ClientName}");
}