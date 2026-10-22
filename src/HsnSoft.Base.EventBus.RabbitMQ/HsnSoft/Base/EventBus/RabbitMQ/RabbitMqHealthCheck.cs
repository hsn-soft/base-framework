using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HsnSoft.Base.EventBus.RabbitMQ;

internal sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqConnection _connection;

    public RabbitMqHealthCheck(RabbitMqConnection connection) => _connection = connection;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(_connection.IsConnected ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("not connected"));
}
