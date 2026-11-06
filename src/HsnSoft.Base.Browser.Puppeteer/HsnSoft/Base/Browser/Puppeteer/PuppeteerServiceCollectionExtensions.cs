using System;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.Browser.Puppeteer;

public static class PuppeteerServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Chrome <see cref="IBrowserRenderer"/> (one for the whole application, closed when the host stops) and a readiness check (the Chrome is there). Chrome is not started until the first page is rendered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the <c>Browser</c> section (<see cref="PuppeteerOptions"/>): <c>ExecutablePath</c> is required.</param>
    public static IServiceCollection AddPuppeteerBrowser(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<PuppeteerOptions>(configuration.GetSection(PuppeteerOptions.SectionName));
        services.TryAddSingleton<PuppeteerBrowserRenderer>();
        services.TryAddSingleton<IBrowserRenderer>(provider => provider.GetRequiredService<PuppeteerBrowserRenderer>());
        services.AddHealthChecks().AddCheck<PuppeteerHealthCheck>("browser", tags: [HealthCheckTags.Ready]);
        return services;
    }
}
