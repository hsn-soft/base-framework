using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace HsnSoft.Base.Caching.StackExchangeRedis;

public class CustomRedisHealthCheck : IHealthCheck
{
    [CanBeNull]
    private readonly string _redisConnectionString;

    [CanBeNull]
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public CustomRedisHealthCheck([CanBeNull] string redisConnectionString)
    {
        _redisConnectionString = redisConnectionString ?? throw new ArgumentNullException(nameof(redisConnectionString));
    }

    public CustomRedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connection = _connectionMultiplexer;
        try
        {
            if (_redisConnectionString is not null)
            {
                try
                {
                    connection = await ConnectionMultiplexer.ConnectAsync(_redisConnectionString);
                }
                catch (OperationCanceledException)
                {
                    return new HealthCheckResult(context.Registration.FailureStatus, description: "Healthcheck timed out");
                }
            }

            bool isActiveConnectionFound = false;
            bool standAloneServerFound = false;
            foreach (var endPoint in connection!.GetEndPoints(configuredOnly: true))
            {
                try
                {
                    var server = connection.GetServer(endPoint);
                    if (server.ServerType != ServerType.Standalone) continue;
                    standAloneServerFound = true;

                    await server.PingAsync();

                    var redisDb = connection.GetDatabase();
                    await redisDb.PingAsync();

                    isActiveConnectionFound = true;
                    break;
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            if (!standAloneServerFound)
            {
                return HealthCheckResult.Healthy();
            }

            if (!isActiveConnectionFound) throw new Exception("Connection not available");

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            if (_redisConnectionString is not null)
            {
                connection?.Dispose();
            }

            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}