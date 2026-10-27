using Abc.ProductService.Domain.ProductDomain.Consts;
using Abc.ProductService.Domain.ProductDomain.Entities;
using Abc.ProductService.Domain.ProductDomain.Repositories;
using Abc.ProductService.MongoDB.Configurations;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Abc.ProductService.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the product service needs of MongoDB: the connection (<c>AddBaseMongo</c>, <c>ConnectionStrings:Mongo-ProductService</c>, and its readiness check), the mappings and the repositories.
    /// </summary>
    public static IServiceCollection AddProductPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseMongo(configuration, MongoProductDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyMongo();

        ProductClassMap.Register();
        services.AddScoped<IProductRepository>(provider => new MongoProductRepository(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoProductDbProperties.CollectionPrefix + ProductConsts.CollectionName));
        services.AddScoped<IRepository<Product, Guid>>(provider => provider.GetRequiredService<IProductRepository>());
        return services;
    }
}
