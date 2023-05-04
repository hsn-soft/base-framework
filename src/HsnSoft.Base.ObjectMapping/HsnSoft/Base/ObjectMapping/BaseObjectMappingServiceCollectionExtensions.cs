using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.ObjectMapping;

public static class BaseObjectMappingServiceCollectionExtensions
{
    public static IServiceCollection AddBaseObjectMappingServiceCollection(this IServiceCollection services)
    {
        // context.Services.OnExposing(onServiceExposingContext =>
        // {
        //     //Register types for IObjectMapper<TSource, TDestination> if implements
        //     onServiceExposingContext.ExposedTypes.AddRange(
        //         ReflectionHelper.GetImplementedGenericTypes(
        //             onServiceExposingContext.ImplementationType,
        //             typeof(IObjectMapper<,>)
        //         )
        //     );
        // });

        services.AddTransient(typeof(IObjectMapper<>), typeof(DefaultObjectMapper<>));

        return services;
    }
}