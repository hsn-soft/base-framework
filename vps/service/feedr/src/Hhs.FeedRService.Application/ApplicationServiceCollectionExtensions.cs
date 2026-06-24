using Hhs.FeedRService.Application.Contracts;
using Hhs.FeedRService.Application.Contracts.CustomerDomain;
using Hhs.FeedRService.Application.Contracts.DashboardDomain;
using Hhs.FeedRService.Application.Contracts.JobDomain;
using Hhs.FeedRService.Application.Infrastructure;
using Hhs.FeedRService.Application.Services;
using Hhs.Shared.Contracts.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.FeedRService.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddServiceApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(ApplicationAutoMapperProfile));
        services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();

        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<ApplicationEventInboxMessageManager>();
        services.AddScoped<FeedROperationRetryWorkerService>();

        services.AddScoped<IJobAppService, JobAppService>();
        services.AddScoped<IEventManagerAppService, EventManagerAppService>();
        services.AddScoped<IGoogleReportService, ReportService>();
        services.AddScoped<IReportPersistenceService, ReportPersistenceService>();
        services.AddScoped<IReportQueryService, ReportQueryService>();
        services.AddScoped<ICustomerConfigurationAppService, CustomerConfigurationAppService>();
        services.AddScoped<INetworkConfigurationAppService, NetworkConfigurationAppService>();
        return services;
    }
}