using HsnSoft.Base.Application;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Services;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Services;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Services;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Services;
using Hsx.IdentityService.Application.Contracts.MenuDomain.Services;
using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Services;
using Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Services;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Services;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Services;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Services;
using Hsx.IdentityService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hsx.IdentityService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the identity service. The repositories come from the persistence project.</summary>
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // the most a client may ask for in a list (the Paging section)
        services.AddBasePaging(configuration);
        services.AddScoped<ICompanyAppService, CompanyAppService>();
        services.AddScoped<ICustomerAppService, CustomerAppService>();
        services.AddScoped<ISubscriptionAppService, SubscriptionAppService>();
        services.AddScoped<ITenantAppService, TenantAppService>();
        services.AddScoped<IMyAccountAppService, MyAccountAppService>();
        services.AddScoped<ITenantManagementAppService, TenantManagementAppService>();
        services.AddScoped<IMySubscriptionAppService, MySubscriptionAppService>();
        services.AddScoped<IPermissionAppService, PermissionAppService>();
        services.AddScoped<IRolePermissionAppService, RolePermissionAppService>();
        services.AddScoped<ISessionPermissionAppService, SessionPermissionAppService>();
        services.AddScoped<IMenuAppService, MenuAppService>();
        services.AddScoped<PermissionCacheSynchronizer>();
        services.AddScoped<IAppUserAppService, AppUserAppService>();
        services.AddScoped<IAppRoleAppService, AppRoleAppService>();
        return services;
    }
}
