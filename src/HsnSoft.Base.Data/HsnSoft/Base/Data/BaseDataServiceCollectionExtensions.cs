using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Data;

public static class BaseDataServiceCollectionExtensions
{
    public static IServiceCollection AddBaseDataServiceCollection(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IDataFilter<>), typeof(DataFilter<>));
        services.AddSingleton(typeof(IDataFilter), typeof(DataFilter));

        return services;
    }
}