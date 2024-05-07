using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using HsnSoft.Base.Timing;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Auditing;

public static class BaseAuditingServiceCollectionExtensions
{
    public static IServiceCollection AddBaseAuditingServiceCollection(this IServiceCollection services)
    {
        // required dependencies
        services.AddBaseMultiTenancyServiceCollection();
        services.AddBaseTimingServiceCollection();

        // auditing dependencies
        services.AddTransient<IAuditPropertySetter, AuditPropertySetter>();

        return services;
    }
}