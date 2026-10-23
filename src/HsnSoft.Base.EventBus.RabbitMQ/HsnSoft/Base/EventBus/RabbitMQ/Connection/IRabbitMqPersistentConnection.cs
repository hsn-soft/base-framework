using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace HsnSoft.Base.EventBus.RabbitMQ.Connection;

/// <summary>One long-lived connection to the broker that recovers by itself when the network breaks.</summary>
public interface IRabbitMqPersistentConnection : IDisposable
{
    bool IsConnected { get; }

    /// <summary>Connects, repeating while the broker is not reachable; throws when it still is not after the configured retries.</summary>
    Task<bool> TryConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Opens a channel. <paramref name="consumerDispatchConcurrency"/> is how many deliveries the client hands to the consumer at the same time.</summary>
    Task<IChannel> CreateModelAsync(ushort? consumerDispatchConcurrency = null, CancellationToken cancellationToken = default);
}
