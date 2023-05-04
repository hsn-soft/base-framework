namespace HsnSoft.Base.EventBus.RabbitMQ;

public class RabbitMQConnectionSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}

public class RabbitMQEventBusSettings
{
    public string ClientName { get; set; } = "eShop_ClientName";
    public string ExchangeName { get; set; } = "eShop";
    public int ConnectionRetryCount { get; set; } = 5;
    public string EventNamePrefix { get; set; } = "";
    public string EventNameSuffix { get; set; } = "IntegrationEvent";
}


// public class BaseRabbitMqEventBusOptions
// {
//     public const string DefaultExchangeType = RabbitMqConsts.ExchangeTypes.Direct;
//
//     public string ConnectionName { get; set; }
//
//     public string ClientName { get; set; }
//
//     public string ExchangeName { get; set; }
//
//     public string ExchangeType { get; set; }
//
//     public string GetExchangeTypeOrDefault()
//     {
//         return string.IsNullOrEmpty(ExchangeType)
//             ? DefaultExchangeType
//             : ExchangeType;
//     }
// }