using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Auditing;

public static class BaseAuditingServiceCollectionExtensions
{
    public static IServiceCollection AddBaseAuditingServiceCollection(this IServiceCollection services)
    {
        services.AddTransient<IAuditPropertySetter, AuditPropertySetter>();

        return services;
    }
}