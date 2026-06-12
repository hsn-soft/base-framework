using Hhs.Shared.Contracts.Cache;
using Hhs.TextNormalizerService.Application.Contracts;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.TextNormalizerService.Application.Contracts.DashboardDomain;
using Hhs.TextNormalizerService.Application.Contracts.Providers;
using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline.OpenAI;
using Hhs.TextNormalizerService.Application.Providers;
using Hhs.TextNormalizerService.Application.Services;
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
        services.Configure<OpenAiSettings>(configuration.GetSection(nameof(OpenAiSettings)));

        services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();
        services.AddSingleton<IPuppeteerBrowser, PuppeteerBrowser>();

        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<IEventManagerAppService, EventManagerAppService>();

        services.AddScoped<IScrapingProvider, PuppeTeerScrapingProvider>();
        services.AddScoped<IOutlineProvider, OpenAiOutlineProvider>();

        services.AddScoped<INormalizedRequestAppService, NormalizedRequestAppService>();
        services.AddScoped<INormalizedAnalysisAppService, NormalizedAnalysisAppService>();
        services.AddScoped<IDashboardAppService, DashboardAppService>();

        return services;
    }
}