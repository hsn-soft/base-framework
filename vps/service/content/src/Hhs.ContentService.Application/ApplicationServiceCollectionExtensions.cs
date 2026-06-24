using Hhs.ContentService.Application.Infrastructure;
using Hhs.ContentService.Application.Services;
using Hhs.ContentService.Domain.Configuration;
using Hhs.ContentService.Domain.Settings;
using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Helper.Retry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.ContentService.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddServiceApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(ApplicationAutoMapperProfile));

        services.Configure<ContentOperationSettings>(configuration.GetSection(nameof(ContentOperationSettings)));
        services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();

        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<ApplicationEventInboxMessageManager>();
        services.AddScoped<ContentOperationAppService>();

        // ============================================================================
        // RETRY CONFIGURATION
        // ============================================================================

        var contentRetrySettings = configuration.GetSection(nameof(ContentRetrySettings))
            .Get<ContentRetrySettings>() ?? new ContentRetrySettings();
        services.AddSingleton(contentRetrySettings);
        services.AddSingleton(_ => new RetryDelayCalculator(contentRetrySettings.DelaySeconds));

        return services;
    }
}