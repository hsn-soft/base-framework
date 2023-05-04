using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Threading;

public static class BaseThreadingServiceCollectionExtensions
{
    public static IServiceCollection AddBaseThreadingServiceCollection(this IServiceCollection services)
    {
        services.AddSingleton<ICancellationTokenProvider>(NullCancellationTokenProvider.Instance);
        services.AddSingleton(typeof(IAmbientScopeProvider<>), typeof(AmbientDataContextAmbientScopeProvider<>));

        return services;
    }
}