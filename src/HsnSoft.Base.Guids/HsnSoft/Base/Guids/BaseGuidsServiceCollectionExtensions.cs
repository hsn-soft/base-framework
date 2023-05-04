using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Guids;

public static class BaseGuidsServiceCollectionExtensions
{
    public static IServiceCollection AddBaseGuidsServiceCollection(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IGuidGenerator), typeof(SimpleGuidGenerator));

        return services;
    }
}