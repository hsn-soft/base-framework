using System;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Timing;

public static class BaseTimingServiceCollectionExtensions
{
    public static IServiceCollection AddBaseTimingServiceCollection(this IServiceCollection services)
    {
        services.AddTransient<IClock, Clock>();

        services.Configure<BaseClockOptions>(o => o.Kind = DateTimeKind.Utc);

        return services;
    }
}