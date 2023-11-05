using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Serialization;

public static class BaseSerializationServiceCollectionExtensions
{
    public static IServiceCollection AddBaseSerializationServiceCollection(this IServiceCollection services)
    {
        services.AddTransient<IObjectSerializer, DefaultObjectSerializer>();

        return services;
    }
}