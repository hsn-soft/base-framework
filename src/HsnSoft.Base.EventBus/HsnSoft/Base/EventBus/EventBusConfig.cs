namespace HsnSoft.Base.EventBus;

public class EventBusConfig
{
    public string ExchangeName { get; set; } = "HsnSoft";
    public string ConsumerClientName { get; set; } = "Client";
    public string ErrorClientName { get; set; } = null;

    public string EventNamePrefix { get; set; } = "";
    public string EventNameSuffix { get; set; } = "Eto";

    public bool DeleteEventPrefix => !string.IsNullOrEmpty(EventNamePrefix);

    public bool DeleteEventSuffix => !string.IsNullOrEmpty(EventNameSuffix);

    public string ConsumerClientInfo =>
        (string.IsNullOrWhiteSpace(ExchangeName) ? string.Empty : $"{ExchangeName}_") +
        (string.IsNullOrWhiteSpace(ConsumerClientName) ? string.Empty : $"{ConsumerClientName}");

    public string ErrorClientInfo =>
        (string.IsNullOrWhiteSpace(ExchangeName) ? string.Empty : $"{ExchangeName}_") +
        (
            string.IsNullOrWhiteSpace(ErrorClientName)
                ? string.IsNullOrWhiteSpace(ConsumerClientName) ? string.Empty : $"{ConsumerClientName}"
                : $"{ErrorClientName}"
        );
}