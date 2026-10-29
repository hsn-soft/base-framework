using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace HsnSoft.Base.Caching.StackExchangeRedis;

internal sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis = redis;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        TimeSpan latency = await _redis.GetDatabase().PingAsync().ConfigureAwait(false);
        return HealthCheckResult.Healthy($"ping {latency.TotalMilliseconds:F0} ms");
    }
}
