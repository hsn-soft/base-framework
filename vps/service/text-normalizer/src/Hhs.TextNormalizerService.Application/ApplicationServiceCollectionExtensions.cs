using Hhs.Shared.Contracts.Cache;
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

        return services;
    }
}