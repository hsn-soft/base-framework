using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.MultiTenancy;

public static class BaseMultiTenancyServiceCollectionExtensions
{
    public static IServiceCollection AddBaseMultiTenancyServiceCollection(this IServiceCollection services)
    {
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        // services.AddScoped<ICurrentTenantAccessor>(sp => AsyncLocalCurrentTenantAccessor.Instance);
        services.AddScoped<ICurrentTenantAccessor>(sp=> new BasicCurrentTenantAccessor(sp));

        return services;
    }
}