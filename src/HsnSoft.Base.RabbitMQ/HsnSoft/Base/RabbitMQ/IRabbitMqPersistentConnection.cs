using System;
using JetBrains.Annotations;
using RabbitMQ.Client;

namespace HsnSoft.Base.RabbitMQ;

public interface IRabbitMqPersistentConnection : IDisposable
{
    bool IsConnected { get; }

    bool TryConnect();

    [CanBeNull]
    IModel CreateModel();
}