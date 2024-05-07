using HsnSoft.Base.Security;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.MultiTenancy;

public static class BaseMultiTenancyServiceCollectionExtensions
{
    public static IServiceCollection AddBaseMultiTenancyServiceCollection(this IServiceCollection services)
    {
        services.AddBaseSecurityServiceCollection();

        // services.AddTransient<ICurrentTenantAccessor>(sp => AsyncLocalCurrentTenantAccessor.Instance);
        services.AddTransient<ICurrentTenantAccessor>(sp=> new BasicCurrentTenantAccessor(sp));

        services.AddTransient<ICurrentTenant, CurrentTenant>();
        return services;
    }
}