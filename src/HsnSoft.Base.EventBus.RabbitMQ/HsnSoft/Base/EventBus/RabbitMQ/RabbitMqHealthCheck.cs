using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace HsnSoft.Base.EventBus.RabbitMQ;

public class RabbitMqHealthCheck(RabbitMqConnectionSettings conSettings) : IHealthCheck
{
    private readonly ConnectionFactory _connectionFactory = new()
    {
        HostName = conSettings.HostName,
        Port = conSettings.Port,
        UserName = conSettings.UserName,
        Password = conSettings.Password,
        VirtualHost = conSettings.VirtualHost,
        RequestedHeartbeat = TimeSpan.FromSeconds(60)
    };

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool connectionOpened = false;
        string errorMessage = string.Empty;
        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            if (connection is { IsOpen: true })
            {
                connectionOpened = true;
            }
        }
        catch (Exception ex)
        {
            connectionOpened = false;
            errorMessage = ex.Message;
        }

        return connectionOpened ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy(errorMessage);
    }
}