using HsnSoft.Base.Data;
using HsnSoft.Base.MongoDB;
using Hsx.TextNormalizerService.Domain.CustomerContentNormalizedRequestDomain.Consts;
using Hsx.TextNormalizerService.Domain.CustomerContentNormalizedRequestDomain.Entities;
using Hsx.TextNormalizerService.MongoDB.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Hsx.TextNormalizerService.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the text normalizer service needs of MongoDB: the connection (<c>AddBaseMongo</c>, <c>ConnectionStrings:Mongo-TextNormalizerService</c>, and its readiness check), the mappings and the repositories, and the start step that makes the indexes.
    /// </summary>
    public static IServiceCollection AddTextNormalizerPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseMongo(configuration, MongoTextNormalizerDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyMongo();
        CustomerContentNormalizedRequestClassMap.Register();
        services.AddMongoRepository<CustomerContentNormalizedRequest, Guid>(MongoTextNormalizerDbProperties.CollectionPrefix + CustomerContentNormalizedRequestConsts.TableName);
        services.AddDatabaseStartupStep(MongoTextNormalizerDbProperties.ConnectionStringName, "TextNormalizer collections", EnsureCollections);
        return services;
    }

    /// <summary>Makes the indexes (the scope is looked up, newest first). MongoDB creates the database and the collection when the first document is written.</summary>
    internal static async Task EnsureCollections(IServiceProvider services, CancellationToken cancellationToken)
    {
        IMongoDatabase database = services.GetRequiredService<IMongoDatabase>();

        IMongoCollection<CustomerContentNormalizedRequest> customercontentnormalizedrequests = database.GetCollection<CustomerContentNormalizedRequest>(MongoTextNormalizerDbProperties.CollectionPrefix + CustomerContentNormalizedRequestConsts.TableName);
        await customercontentnormalizedrequests.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<CustomerContentNormalizedRequest>(Builders<CustomerContentNormalizedRequest>.IndexKeys.Ascending(entity => entity.ScopeKey).Descending(entity => entity.CreatedAt)),
            ],
            cancellationToken).ConfigureAwait(false);
    }
}
