using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Xyz.Shared.Hosting;

public static partial class ServiceRegister
{
    /// <summary>The distributed cache (<c>IDistributedCache</c>) and the raw <see cref="IConnectionMultiplexer"/>. Connection string: <c>ConnectionStrings:Redis</c>.</summary>
    public static IServiceCollection AddSharedRedis(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Redis")
                                  ?? throw new InvalidOperationException("ConnectionStrings:Redis is required.");

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = "xyz:";
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false; // the service may start before redis does
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
        return services;
    }
}
