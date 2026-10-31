using Abc.ProductService.Domain.ProductStockHistoryDomain.Consts;
using Abc.ProductService.Domain.ProductStockHistoryDomain.Entities;
using Abc.ProductService.MongoDB.Configurations;
using HsnSoft.Base.Data;
using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Abc.ProductService.MongoDB;

public static class StockMongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers the stock history of the product service on its own MongoDB (<c>ConnectionStrings:Mongo-StockService</c>, under the key <see cref="MongoStockDbProperties.ServiceKey"/>, with its readiness check), beside the one of
    /// <see cref="MongoServiceCollectionExtensions.AddProductPersistence"/>, with the mapping and the repository.
    /// </summary>
    public static IServiceCollection AddProductStockPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseMongo(configuration, MongoStockDbProperties.ConnectionStringName, MongoStockDbProperties.ServiceKey);
        services.AddBaseMultiTenancyMongo();

        ProductStockHistoryClassMap.Register();
        services.AddMongoRepository<ProductStockHistory, Guid>(MongoStockDbProperties.CollectionPrefix + ProductStockHistoryConsts.CollectionName, MongoStockDbProperties.ServiceKey);
        services.AddDatabaseStartupStep(MongoStockDbProperties.ConnectionStringName, "Product stock history collection", EnsureProductStockCollection);
        return services;
    }

    /// <summary>Makes the indexes of the stock history (the product, the tenant, the time). MongoDB creates the database and the collection when the first record is written.</summary>
    internal static async Task EnsureProductStockCollection(IServiceProvider services, CancellationToken cancellationToken)
    {
        IMongoCollection<ProductStockHistory> histories = services.GetRequiredKeyedService<IMongoDatabase>(MongoStockDbProperties.ServiceKey)
            .GetCollection<ProductStockHistory>(MongoStockDbProperties.CollectionPrefix + ProductStockHistoryConsts.CollectionName);
        await histories.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<ProductStockHistory>(Builders<ProductStockHistory>.IndexKeys.Ascending(history => history.ProductId).Descending(history => history.CreatedAt)),
                new CreateIndexModel<ProductStockHistory>(Builders<ProductStockHistory>.IndexKeys.Ascending(history => history.TenantId)),
            ],
            cancellationToken).ConfigureAwait(false);
    }
}
