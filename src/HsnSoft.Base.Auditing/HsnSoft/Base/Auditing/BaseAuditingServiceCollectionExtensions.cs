using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Timing;
using HsnSoft.Base.Users;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Auditing;

public static class BaseAuditingServiceCollectionExtensions
{
    public static IServiceCollection AddBaseAuditingServiceCollection(this IServiceCollection services)
    {
        services.AddBaseMultiTenancyServiceCollection();
        services.AddBaseTimingServiceCollection();
        services.AddTransient<IAuditPropertySetter, AuditPropertySetter>();

        return services;
    }
}