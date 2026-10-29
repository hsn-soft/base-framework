using Edx.Mock.Center.Domain.Envelopes;
using Edx.Mock.Center.Domain.Participants;
using Edx.Mock.Center.MongoDB.Configurations;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Hosting;
using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Edx.Mock.Center.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>Registers what the mock center needs of MongoDB: the connection (<c>AddBaseMongo</c>, <c>ConnectionStrings:Mongo-MockCenter</c>), the mapping and the two repositories.</summary>
    public static IServiceCollection AddCenterPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseMongo(configuration, MongoCenterDbProperties.ConnectionStringName);
        CenterClassMaps.Register();
        services.AddScoped<IParticipantRepository>(provider => new MongoParticipantRepository(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoCenterDbProperties.CollectionPrefix + MongoCenterDbProperties.ParticipantCollection));
        services.AddScoped<IRepository<Participant, Guid>>(provider => provider.GetRequiredService<IParticipantRepository>());
        services.AddScoped<IEnvelopeQueueRepository>(provider => new MongoEnvelopeQueueRepository(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoCenterDbProperties.CollectionPrefix + MongoCenterDbProperties.EnvelopeCollection));
        services.AddScoped<IRepository<EnvelopeQueueItem, Guid>>(provider => provider.GetRequiredService<IEnvelopeQueueRepository>());
        return services;
    }

    /// <summary>Makes the indexes: a participant is found by its register number (unique), an envelope by its uuid (unique, which is what refuses the same envelope twice) and the workers find theirs by stage.</summary>
    public static Task EnsureCenterDatabaseAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.RunStartupStepAsync("Center collections", async cancellationToken =>
        {
            IMongoDatabase database = services.GetRequiredService<IMongoDatabase>();
            await database.GetCollection<Participant>(MongoCenterDbProperties.CollectionPrefix + MongoCenterDbProperties.ParticipantCollection).Indexes.CreateOneAsync(
                new CreateIndexModel<Participant>(Builders<Participant>.IndexKeys.Ascending(participant => participant.RegisterNumber), new CreateIndexOptions { Unique = true }),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            await database.GetCollection<EnvelopeQueueItem>(MongoCenterDbProperties.CollectionPrefix + MongoCenterDbProperties.EnvelopeCollection).Indexes.CreateManyAsync(
                [
                    new CreateIndexModel<EnvelopeQueueItem>(Builders<EnvelopeQueueItem>.IndexKeys.Ascending(item => item.EnvelopeId), new CreateIndexOptions { Unique = true }),
                    new CreateIndexModel<EnvelopeQueueItem>(Builders<EnvelopeQueueItem>.IndexKeys.Ascending(item => item.Stage).Ascending(item => item.NextSendAt)),
                    new CreateIndexModel<EnvelopeQueueItem>(Builders<EnvelopeQueueItem>.IndexKeys.Ascending(item => item.DocumentKeys)),
                ],
                cancellationToken).ConfigureAwait(false);
        });
    }
}
