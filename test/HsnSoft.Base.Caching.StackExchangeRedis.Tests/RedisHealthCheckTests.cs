using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace HsnSoft.Base.Caching.StackExchangeRedis.Tests;

public class RedisHealthCheckTests
{
    /// <summary>A multiplexer whose database answers the ping with what the test gives (a result, a failure, or never).</summary>
    public class Multiplexer : DispatchProxy
    {
        public bool Connected { get; set; } = true;

        public Func<Task<TimeSpan>> Ping { get; set; } = () => Task.FromResult(TimeSpan.FromMilliseconds(3));

        public int Pings { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod!.Name)
            {
                case "get_IsConnected":
                    return Connected;
                case "GetDatabase":
                    IDatabase database = Create<IDatabase, Database>();
                    ((Database)(object)database).Owner = this;
                    return database;
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }

        public void Pinged() => Pings++;
    }

    public class Database : DispatchProxy
    {
        public Multiplexer? Owner { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name != "PingAsync")
            {
                throw new NotSupportedException(targetMethod.Name);
            }

            Owner!.Pinged();
            return Owner.Ping();
        }
    }

    private static (RedisHealthCheck Check, Multiplexer Multiplexer) Build(Action<Multiplexer>? configure = null)
    {
        IConnectionMultiplexer proxy = Create<IConnectionMultiplexer, Multiplexer>();
        Multiplexer multiplexer = (Multiplexer)(object)proxy;
        configure?.Invoke(multiplexer);
        return (new RedisHealthCheck(proxy), multiplexer);
    }

    private static TInterface Create<TInterface, TProxy>()
        where TProxy : DispatchProxy
        => DispatchProxy.Create<TInterface, TProxy>();

    [Fact]
    public async Task ARedisThatAnswers_IsHealthy_WithItsLatency()
    {
        (RedisHealthCheck check, Multiplexer multiplexer) = Build();

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("ping 3 ms", result.Description);
        Assert.Equal(1, multiplexer.Pings);
    }

    [Fact]
    public async Task AConnectionThatIsDown_IsUnhealthyAtOnce_WithoutAPing()
    {
        (RedisHealthCheck check, Multiplexer multiplexer) = Build(m => m.Connected = false);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal(0, multiplexer.Pings);
    }

    [Fact]
    public async Task AFailedPing_IsUnhealthy_AndTheDescriptionHasNoAddress()
    {
        InvalidOperationException failure = new("redis-3.internal:6379 timed out");
        (RedisHealthCheck check, _) = Build(m => m.Ping = () => Task.FromException<TimeSpan>(failure));

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.DoesNotContain("redis-3", result.Description);
        Assert.Same(failure, result.Exception);
    }

    [Fact]
    public async Task APingThatNeverEnds_StopsWithTheTokenOfTheCheck()
    {
        (RedisHealthCheck check, _) = Build(m => m.Ping = () => new TaskCompletionSource<TimeSpan>().Task);
        using CancellationTokenSource timeout = new(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => check.CheckHealthAsync(new HealthCheckContext(), timeout.Token));
    }
}
