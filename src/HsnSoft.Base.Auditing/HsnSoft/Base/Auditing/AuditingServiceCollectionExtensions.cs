using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.Auditing;

public static class AuditingServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IAuditPropertySetter"/> and the clock it reads (<see cref="TimeProvider.System"/>); what the host registered before is kept. The user comes from <see cref="Security.ICurrentUser"/>,
    /// which the JWT authentication registers: without one, the user fields stay empty.
    /// </summary>
    public static IServiceCollection AddBaseAuditing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IAuditPropertySetter, AuditPropertySetter>();
        return services;
    }
}
