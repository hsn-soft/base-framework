using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Helper.Retry;
using Hhs.TextNormalizerService.Application.Infrastructure;
using Hhs.TextNormalizerService.Application.Providers;
using Hhs.TextNormalizerService.Application.Providers.Outline;
using Hhs.TextNormalizerService.Application.Providers.Scraping;
using Hhs.TextNormalizerService.Application.Services;
using Hhs.TextNormalizerService.Domain.Configuration;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;
using Hhs.TextNormalizerService.Domain.Settings;
using HsnSoft.Base.PuppeTeer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.TextNormalizerService.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddServiceApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(ApplicationAutoMapperProfile));

        services.Configure<TextNormalizerSettings>(configuration.GetSection(nameof(TextNormalizerSettings)));
        services.Configure<PuppeteerBrowserSettings>(configuration.GetSection(nameof(PuppeteerBrowserSettings)));

        services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();
        services.AddSingleton<IPuppeteerBrowser, PuppeteerBrowser>();

        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<NormalizerInboxStoreService>();
        services.AddScoped<NormalizerOperationAppService>();
        services.AddScoped<IContentScraper, DummyContentScraper>();

        // ============================================================================
        // 2. OUTLINE PROVIDER CONFIGURATION
        // ============================================================================

        var outlineFastProviderSettings = configuration.GetSection(OutlineFastProviderSettings.SectionName)
            .Get<OutlineFastProviderSettings>() ?? new OutlineFastProviderSettings();
        services.AddSingleton(outlineFastProviderSettings);

        var outlineQueueProviderSettings = configuration.GetSection(OutlineQueueProviderSettings.SectionName)
            .Get<OutlineQueueProviderSettings>() ?? new OutlineQueueProviderSettings();
        services.AddSingleton(outlineQueueProviderSettings);

        // Register Outline Provider implementations
        services.AddScoped<IOutlineProvider, OutlineFastProvider>();
        services.AddScoped<IOutlineProvider, OutlineQueueProvider>();
        services.AddScoped<IOutlineProviderResolver, OutlineProviderResolver>();

        // ============================================================================
        // 3. POLLING & RETRY CONFIGURATION
        // ============================================================================

        var outlinePollingSettings = configuration.GetSection(OutlinePollingSettings.SectionName)
            .Get<OutlinePollingSettings>() ?? new OutlinePollingSettings();
        services.AddSingleton(outlinePollingSettings);

        var normalizerRetrySettings = configuration.GetSection(nameof(NormalizerRetrySettings))
            .Get<NormalizerRetrySettings>() ?? new NormalizerRetrySettings();
        services.AddSingleton(normalizerRetrySettings);
        services.AddSingleton(_ => new RetryDelayCalculator(normalizerRetrySettings.DelaySeconds));

        return services;
    }
}