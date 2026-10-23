using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HsnSoft.Base.EventBus.RabbitMQ;

internal sealed class RabbitMqHealthCheck(RabbitMqConnection connection) : IHealthCheck
{
    private readonly RabbitMqConnection _connection = connection;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(_connection.IsConnected ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("not connected"));
}
