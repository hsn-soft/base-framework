using HsnSoft.Base.Security;
using HsnSoft.Base.Timing;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Auditing;

public static class BaseAuditingServiceCollectionExtensions
{
    public static IServiceCollection AddBaseAuditingServiceCollection(this IServiceCollection services)
    {
        services.AddBaseSecurityServiceCollection();
        services.AddBaseTimingServiceCollection();
        services.AddTransient<IAuditPropertySetter, AuditPropertySetter>();

        return services;
    }
}