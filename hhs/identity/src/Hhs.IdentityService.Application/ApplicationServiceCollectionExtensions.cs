using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Services;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Services;
using Hhs.IdentityService.Application.Services;
using Hhs.Shared.Contracts.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.IdentityService.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddServiceApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(ApplicationAutoMapperProfile));

        services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();

        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<IAppUserAppService, AppUserAppService>();
        services.AddScoped<IAppRoleAppService, AppRoleAppService>();

        return services;
    }
}