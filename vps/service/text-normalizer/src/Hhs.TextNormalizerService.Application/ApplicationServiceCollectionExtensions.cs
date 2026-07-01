using Hhs.Shared.Contracts.Cache;
using Hhs.TextNormalizerService.Application.Infrastructure;
using Hhs.TextNormalizerService.Application.Providers;
using Hhs.TextNormalizerService.Application.Providers.Outline;
using Hhs.TextNormalizerService.Application.Providers.Scraping;
using Hhs.TextNormalizerService.Application.Services;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;
using Hhs.TextNormalizerService.Domain.Settings;
using HsnSoft.Base.PuppeTeer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hhs.TextNormalizerService.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddServiceApplicationConfiguration(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddAutoMapper(typeof(ApplicationAutoMapperProfile));

        services.Configure<TextNormalizerSettings>(configuration.GetSection(nameof(TextNormalizerSettings)));
        services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();

        services.Configure<PuppeteerBrowserSettings>(configuration.GetSection(nameof(PuppeteerBrowserSettings)));
        services.AddSingleton<IPuppeteerBrowser, PuppeteerBrowser>();

        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<ApplicationEventInboxMessageManager>();
        services.AddScoped<NormalizerOperationAppService>();

        if (environment.IsProduction())
            services.AddScoped<IContentScraper, PuppeteerContentScraper>();
        else
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

        var outlineOpenAiProviderSettings = configuration.GetSection(OutlineOpenAiProviderSettings.SectionName)
            .Get<OutlineOpenAiProviderSettings>() ?? new OutlineOpenAiProviderSettings();
        services.AddSingleton(outlineOpenAiProviderSettings);

        // Register Outline Provider implementations
        services.AddScoped<IOutlineProvider, OutlineFastProvider>();
        services.AddScoped<IOutlineProvider, OutlineQueueProvider>();
        services.AddScoped<IOutlineProvider, OutlineOpenAiProvider>();
        services.AddScoped<IOutlineProviderResolver, OutlineProviderResolver>();

        return services;
    }
}