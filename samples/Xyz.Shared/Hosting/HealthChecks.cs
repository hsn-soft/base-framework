using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;
using Xyz.Shared.Messaging;

namespace Xyz.Shared.Hosting;

internal sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var latency = await redis.GetDatabase().PingAsync();
        return HealthCheckResult.Healthy($"ping {latency.TotalMilliseconds:F0} ms");
    }
}

internal sealed class RabbitMqHealthCheck(RabbitMqConnection connection) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(connection.IsConnected ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("not connected"));
}

internal sealed class MongoHealthCheck(IMongoDatabase database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
        return HealthCheckResult.Healthy();
    }
}

internal sealed class DbContextHealthCheck<TContext>(TContext context) : IHealthCheck
    where TContext : DbContext
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context1, CancellationToken cancellationToken = default)
        => await context.Database.CanConnectAsync(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("cannot connect");
}
