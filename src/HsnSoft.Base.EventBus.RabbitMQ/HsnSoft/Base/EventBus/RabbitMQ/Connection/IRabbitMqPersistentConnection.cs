using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace HsnSoft.Base.EventBus.RabbitMQ.Connection;

public interface IRabbitMqPersistentConnection : IDisposable
{
    bool IsConnected { get; }

    Task<bool> TryConnectAsync();

    Task<IChannel> CreateModelAsync(ushort? consumerDispatchConcurrency = null);

    Task<int> GetRabbitMqConnectionCountAsync();
}