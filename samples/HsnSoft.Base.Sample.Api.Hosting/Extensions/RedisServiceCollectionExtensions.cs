using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HsnSoft.Base.Sample.Api.Hosting.Extensions;

public static class RedisServiceCollectionExtensions
{
    /// <summary>Registers the distributed cache, lock service and Redis repositories. Setting: <c>Redis:Configuration</c>.</summary>
    public static IServiceCollection AddSampleRedis(this IServiceCollection services, IConfiguration configuration)
    {
        string redisConfiguration = configuration["Redis:Configuration"] ?? throw new InvalidOperationException("Redis:Configuration is required.");

        services.AddStackExchangeRedisCache(options => options.Configuration = redisConfiguration);
        services.AddSingleton<RedisLockService>();

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(redisConfiguration, true)));
        services.AddSingleton(typeof(IRedisRepository<>), typeof(RedisRepository<>));
        services.AddSingleton<IRequestLimitStore, RedisRequestLimitStore>();

        return services;
    }
}
