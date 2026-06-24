using Hhs.EventManagerService.Application.Contracts.EventDomain.Interfaces;
using Hhs.EventManagerService.Application.Infrastructure;
using Hhs.EventManagerService.Application.Services;
using Hhs.EventManagerService.Domain.Configuration;
using Hhs.Shared.Contracts.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.EventManagerService.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddServiceApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(ApplicationAutoMapperProfile));

        services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();

        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<ApplicationEventInboxMessageManager>();
        services.AddScoped<EventOperationRetryWorkerService>();

        services.AddScoped<IFailedIntegrationEventAppService, FailedIntegrationEventAppService>();

        return services;
    }
}