using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.ApiVersioning;

public static class BaseApiVersioningServiceCollectionExtensions
{
    public static IServiceCollection AddBaseApiVersioningServiceCollection(this IServiceCollection services)
    {
        services.AddSingleton<IRequestedApiVersion>(NullRequestedApiVersion.Instance);

        return services;
    }
}