using System;

namespace HsnSoft.Base.EventBus.RabbitMQ.Connection;

/// <summary>There is no open connection to the broker (it is being recovered, or could not be established). It is transient: publishing is tried again.</summary>
public sealed class RabbitMqNotConnectedException : InvalidOperationException
{
    public RabbitMqNotConnectedException()
        : base("RabbitMQ connection could not be established.")
    {
    }
}
