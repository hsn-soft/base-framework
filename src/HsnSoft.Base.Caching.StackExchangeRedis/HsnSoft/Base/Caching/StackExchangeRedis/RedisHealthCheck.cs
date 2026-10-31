using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace HsnSoft.Base.Caching.StackExchangeRedis;

/// <summary>
/// Ready when the connection of the service has a server and the server answers a ping. A connection that is down answers at once (the multiplexer of a service that started before Redis
/// reconnects by itself), the ping ends with the token of the check (the timeout of the readiness probe), and the description has no address: the exception stays on the result.
/// </summary>
internal sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis = redis;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_redis.IsConnected)
        {
            return HealthCheckResult.Unhealthy("Redis is not connected");
        }

        try
        {
            TimeSpan latency = await _redis.GetDatabase().PingAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy($"ping {latency.TotalMilliseconds:F0} ms");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Redis cannot be reached", exception);
        }
    }
}
