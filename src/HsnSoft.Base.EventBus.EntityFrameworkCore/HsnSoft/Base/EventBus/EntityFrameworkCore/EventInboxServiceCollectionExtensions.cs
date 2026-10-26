using System;
using HsnSoft.Base.EventBus.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
}
