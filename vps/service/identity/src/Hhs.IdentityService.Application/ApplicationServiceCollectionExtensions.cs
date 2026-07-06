using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Services;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Services;
using Hhs.IdentityService.Application.Infrastructure;
using Hhs.IdentityService.Application.Services;
using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Helper.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.IdentityService.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddServiceApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(ApplicationAutoMapperProfile));

        services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<IEventInboxMessageManager, ApplicationEventInboxMessageManager>();
        services.AddScoped<IdentityOperationRetryWorkerService>();

        services.AddScoped<IAppUserAppService, AppUserAppService>();
        services.AddScoped<IAppRoleAppService, AppRoleAppService>();

        return services;
    }
}