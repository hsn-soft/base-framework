using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>
/// Ready when the connection of the service to the broker is open. It looks at the connection the service has and opens no other: a probe that opened its own connection every few
/// seconds, in every pod, showed thousands of connections on the broker. A service that has not connected yet (one that only publishes connects at its first publish) connects here, with the one
/// connection it keeps; the timeout of the check ends the attempt, and a connection that is being recovered is not replaced.
/// </summary>
public sealed class RabbitMqHealthCheck(IRabbitMqPersistentConnection connection) : IHealthCheck
{
    private readonly IRabbitMqPersistentConnection _connection = connection;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_connection.IsConnected)
        {
            return HealthCheckResult.Healthy("RabbitMQ is connected");
        }

        try
        {
            return await _connection.TryConnectAsync(cancellationToken).ConfigureAwait(false)
                ? HealthCheckResult.Healthy("RabbitMQ is connected")
                : HealthCheckResult.Unhealthy("RabbitMQ is not connected");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // the exception stays on the result (a host may show it); the description is what every reader may see
            return HealthCheckResult.Unhealthy("RabbitMQ cannot be reached", exception);
        }
    }
}
