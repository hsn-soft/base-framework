using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Caching.StackExchangeRedis;

public static class BaseStackExchangeRedisServiceCollectionExtensions
{
    public static IServiceCollection AddBaseStackExchangeRedisServiceCollection(this IServiceCollection services, IConfiguration configuration)
    {
        //DependsOn
        services.AddBaseCachingServiceCollection();


        services.AddStackExchangeRedisCache(options =>
        {
            var redisConfiguration = configuration["Redis:Configuration"];
            if (!string.IsNullOrEmpty(redisConfiguration))
            {
                options.Configuration = redisConfiguration;
            }
        });
        services.AddSingleton<IDistributedCache, BaseRedisCache>();

        return services;
    }
}