using Hhs.AdministrationService.Application.Contracts.JobDomain;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;
using Hhs.AdministrationService.Application.Services;
using Hhs.Shared.Contracts.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.AdministrationService.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddServiceApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(ApplicationAutoMapperProfile));

        services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();

        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<IJobAppService, JobAppService>();
        services.AddScoped<IPermissionGrantAppService, PermissionGrantAppService>();
        services.AddScoped<IMenuPermissionAppService, MenuPermissionAppService>();
        services.AddScoped<ISessionPermissionAppService, SessionPermissionAppService>();
        services.AddScoped<IRolePermissionAppService, RolePermissionAppService>();

        return services;
    }
}