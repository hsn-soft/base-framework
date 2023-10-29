using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.MultiTenancy;

public static class BaseMultiTenancyServiceCollectionExtensions
{
    public static IServiceCollection AddBaseMultiTenancyServiceCollection(this IServiceCollection services)
    {
        services.AddTransient<ICurrentTenant, CurrentTenant>();
        // services.AddTransient<ICurrentTenantAccessor>(sp => AsyncLocalCurrentTenantAccessor.Instance);
        services.AddTransient<ICurrentTenantAccessor>(sp=> new BasicCurrentTenantAccessor(sp));

        return services;
    }
}