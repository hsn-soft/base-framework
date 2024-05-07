using HsnSoft.Base.Authorization.Permissions;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.Authorization;

public static class BaseAuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddBaseAuthorizationServiceCollection(this IServiceCollection services)
    {
        services.AddAuthorizationCore();

        services.AddBaseMultiTenancyServiceCollection();

        services.AddSingleton<IPermissionStore, BasePermissionStore>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddSingleton<IAuthorizationHandler, PermissionRequirementHandler>();

        services.TryAddTransient<DefaultAuthorizationPolicyProvider>();

        return services;
    }
}