using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Localization;

public static partial class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the localization: the texts (<see cref="LocalizationStore"/>), the current language (<see cref="ILanguageAccessor"/>), <see cref="IMessageLocalizer"/> and the standard
    /// <see cref="IStringLocalizer"/>. The framework's own texts are the first layer; add the others, outermost first (<c>configure: options =&gt; options.AddEmbedded(assembly)</c>): it can be
    /// called more than once, each call adds its layers after the earlier ones. The languages are read from the <c>Localization</c> section. The texts are loaded when the application starts,
    /// so a broken file stops it, and what the translations are missing is logged as warnings.
    /// </summary>
    public static IServiceCollection AddBaseLocalization(this IServiceCollection services, IConfiguration? configuration = null, Action<LocalizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        OptionsBuilder<LocalizationOptions> options = services.AddOptions<LocalizationOptions>();
        if (configuration is not null)
        {
            options.Bind(configuration.GetSection(LocalizationOptions.SectionName));
        }

        if (configure is not null)
        {
            options.Configure(configure);
        }

        services.TryAddSingleton<LocalizationStore>();
        services.TryAddSingleton<ILanguageAccessor, LanguageAccessor>();
        services.TryAddSingleton<IMessageLocalizer, MessageLocalizer>();
        services.TryAddSingleton<IStringLocalizerFactory, StoreStringLocalizerFactory>();
        services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StoreStringLocalizer<>));
        services.TryAddTransient<IStringLocalizer>(provider => provider.GetRequiredService<IStringLocalizerFactory>().Create(typeof(object)));
        services.AddHostedService<LocalizationStartup>();
        return services;
    }

    private sealed partial class LocalizationStartup(LocalizationStore store, ILogger<LocalizationStartup> logger) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (string problem in store.Validate())
            {
                LogProblem(logger, problem);
            }

            return Task.CompletedTask;
        }

        [LoggerMessage(Level = LogLevel.Warning, Message = "Localization: {Problem}")]
        private static partial void LogProblem(ILogger logger, string problem);

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
