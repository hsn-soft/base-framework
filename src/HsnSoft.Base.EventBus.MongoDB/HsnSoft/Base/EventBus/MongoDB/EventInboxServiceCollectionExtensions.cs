using System;
using HsnSoft.Base.EventBus.Inbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HsnSoft.Base.EventBus.MongoDB;

public static class EventInboxServiceCollectionExtensions
{
    /// <summary>
    /// Gives the service an inbox in the collection <paramref name="collectionName"/> of its MongoDB (after <c>AddBaseMongo</c>): every message it handles is handled once, however many times a broker brings it. A service that does not call
    /// this has no inbox and pays nothing for it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="collectionName">The collection of the inbox, named like the other collections of the service.</param>
    /// <param name="serviceKey">The key of the <c>AddBaseMongo</c> that registered the database, when the service has more than one.</param>
    /// <param name="configure">Changes <see cref="EventInboxOptions"/>.</param>
    public static IServiceCollection AddEventInbox(this IServiceCollection services, string collectionName = "InboxMessage", string? serviceKey = null, Action<EventInboxOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

        services.AddOptions<EventInboxOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<InboxEnabled>();
        return services.AddScoped<IInboxStore>(provider => new MongoInboxStore(
            (serviceKey is null ? provider.GetRequiredService<IMongoDatabase>() : provider.GetRequiredKeyedService<IMongoDatabase>(serviceKey)).GetCollection<InboxMessage>(collectionName),
            provider.GetRequiredService<IOptions<EventInboxOptions>>()));
    }
}
