using HsnSoft.Base.Data;
using HsnSoft.Base.MongoDB;
using Hsx.VideoGeneratorService.Domain.VideoRequestDomain.Consts;
using Hsx.VideoGeneratorService.Domain.VideoRequestDomain.Entities;
using Hsx.VideoGeneratorService.MongoDB.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Hsx.VideoGeneratorService.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the video generator service needs of MongoDB: the connection (<c>AddBaseMongo</c>, <c>ConnectionStrings:Mongo-VideoGeneratorService</c>, and its readiness check), the mappings and the repositories, and the start step that makes the indexes.
    /// </summary>
    public static IServiceCollection AddVideoGeneratorPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseMongo(configuration, MongoVideoGeneratorDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyMongo();
        VideoRequestClassMap.Register();
        services.AddMongoRepository<VideoRequest, Guid>(MongoVideoGeneratorDbProperties.CollectionPrefix + VideoRequestConsts.TableName);
        services.AddDatabaseStartupStep(MongoVideoGeneratorDbProperties.ConnectionStringName, "VideoGenerator collections", EnsureCollections);
        return services;
    }

    /// <summary>Makes the indexes (the scope is looked up, newest first). MongoDB creates the database and the collection when the first document is written.</summary>
    internal static async Task EnsureCollections(IServiceProvider services, CancellationToken cancellationToken)
    {
        IMongoDatabase database = services.GetRequiredService<IMongoDatabase>();

        IMongoCollection<VideoRequest> videorequests = database.GetCollection<VideoRequest>(MongoVideoGeneratorDbProperties.CollectionPrefix + VideoRequestConsts.TableName);
        await videorequests.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<VideoRequest>(Builders<VideoRequest>.IndexKeys.Ascending(entity => entity.ScopeKey).Descending(entity => entity.CreatedAt)),
            ],
            cancellationToken).ConfigureAwait(false);
    }
}
