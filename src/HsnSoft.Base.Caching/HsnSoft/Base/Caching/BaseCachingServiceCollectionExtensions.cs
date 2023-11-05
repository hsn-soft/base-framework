using System;
using HsnSoft.Base.Json;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Serialization;
using HsnSoft.Base.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Caching;

public static class BaseCachingServiceCollectionExtensions
{
    public static IServiceCollection AddBaseCachingServiceCollection(this IServiceCollection services)
    {
        //DependsOn
        services.AddBaseThreadingServiceCollection();
        services.AddBaseSerializationServiceCollection();
        services.AddBaseMultiTenancyServiceCollection();
        services.AddBaseJsonServiceCollection();

        //Caching
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();

        services.AddSingleton(typeof(IBaseDistributedCache<>), typeof(BaseDistributedCache<>));
        services.AddSingleton(typeof(IBaseDistributedCache<,>), typeof(BaseDistributedCache<,>));

        services.Configure<BaseDistributedCacheOptions>(cacheOptions =>
        {
            cacheOptions.GlobalCacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(20);
        });

        services.AddTransient<IDistributedCacheKeyNormalizer, DistributedCacheKeyNormalizer>();
        services.AddTransient<IDistributedCacheSerializer, Utf8JsonDistributedCacheSerializer>();

        return services;
    }
}