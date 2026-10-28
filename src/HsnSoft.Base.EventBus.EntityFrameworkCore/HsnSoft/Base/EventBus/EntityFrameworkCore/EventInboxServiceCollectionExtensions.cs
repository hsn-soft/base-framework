using System;
using HsnSoft.Base.EventBus.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.EntityFrameworkCore;

public static class EventInboxServiceCollectionExtensions
{
    /// <summary>
    /// Gives the service an inbox on its context <typeparamref name="TContext"/> (which maps it with <see cref="InboxModelBuilderExtensions.ConfigureEventInbox"/>): every message it handles is handled once, however many times a broker brings it.
    /// A service that does not call this has no inbox and pays nothing for it.
    /// </summary>
    public static IServiceCollection AddEventInbox<TContext>(this IServiceCollection services, Action<EventInboxOptions>? configure = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<EventInboxOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<InboxEnabled>();
        return services.AddScoped<IInboxStore, EfCoreInboxStore<TContext>>();
    }

    /// <summary>
    /// Makes the service forget the messages it handled long ago (<see cref="EventInboxCleanupOptions"/>, section <c>EventInbox:Cleanup</c> when <paramref name="configuration"/> is given): a worker, run by every instance, removes them a batch at a time.
    /// Without this call the table only grows, which is what a service that cleans it in the database itself (a job of the database) wants.
    /// </summary>
    public static IServiceCollection AddEventInboxCleanup<TContext>(this IServiceCollection services, IConfiguration? configuration = null, Action<EventInboxCleanupOptions>? configure = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        OptionsBuilder<EventInboxCleanupOptions> builder = services.AddOptions<EventInboxCleanupOptions>();
        if (configuration is not null)
        {
            builder.Bind(configuration.GetSection(EventInboxCleanupOptions.SectionName));
        }

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        return services.AddHostedService<EfCoreInboxCleanup<TContext>>();
    }
}
