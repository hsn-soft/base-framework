using HsnSoft.Base.Authorization.Permissions;
using HsnSoft.Base.Authorization.Permissions.Store;
using HsnSoft.Base.Authorization.Permissions.ValueProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Authorization;

public static class BaseAuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddBaseAuthorizationServiceCollection(this IServiceCollection services)
    {
        services.AddAuthorizationCore();

        // 1 - Add all policies to authorizations (microservice permissions)
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        // 2 - Authorization permission requirement handler
        services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();

        // 3 - Permission checkers
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddScoped<IPermissionConstraintChecker, PermissionConstraintChecker>();

        // 4 - Authorization permission value control providers
        services.AddScoped<IPermissionValueProvider, RolePermissionValueProvider>();
        services.AddScoped<IPermissionValueProvider, UserPermissionValueProvider>();
        services.AddScoped<IPermissionConstraintValueProvider, UserConstraintValueProvider>();
        services.AddScoped<IPermissionConstraintValueProvider, RoleConstraintValueProvider>();

        // 5 - Permission stores for context user,role or client permissions
        services.AddSingleton<IPermissionStore, BasePermissionStore>();
        services.AddSingleton<IPermissionConstraintStore, BasePermissionConstraintStore>();

        return services;
    }
}