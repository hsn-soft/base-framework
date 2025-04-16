using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RabbitMQ.Client;

namespace HsnSoft.Base.EventBus.RabbitMQ.Connection;

public interface IRabbitMqPersistentConnection : IDisposable
{
    bool IsConnected { get; }

    Task<bool> TryConnectAsync();

    [CanBeNull]
    Task<IChannel> CreateModelAsync();
}