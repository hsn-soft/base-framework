using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>Ready when a fresh connection to the broker can be opened (so it also works for a service that has not published or consumed anything yet).</summary>
public sealed class RabbitMqHealthCheck(IOptions<RabbitMqConnectionSettings> settings) : IHealthCheck
{
    private readonly ConnectionFactory _connectionFactory = new()
    {
        HostName = settings.Value.HostName,
        Port = settings.Value.Port,
        UserName = settings.Value.UserName,
        Password = settings.Value.Password,
        VirtualHost = settings.Value.VirtualHost,
        RequestedHeartbeat = TimeSpan.FromSeconds(60),
    };

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            IConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using (connection.ConfigureAwait(false))
            {
                // opening it is the check: the broker answered and accepted the credentials
            }

            return HealthCheckResult.Healthy("RabbitMQ is ready");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(exception.Message);
        }
    }
}
