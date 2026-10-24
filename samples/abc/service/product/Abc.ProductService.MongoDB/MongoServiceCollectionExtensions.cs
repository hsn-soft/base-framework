using Abc.ProductService.Domain.ProductDomain.Repositories;
using Abc.ProductService.MongoDB.Configurations;
using Abc.ProductService.MongoDB.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.ProductService.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mappings and the repositories of the product service. The <c>IMongoDatabase</c> itself is registered by the host
    /// (<c>AddBaseMongo</c>, connection string <c>ConnectionStrings:Mongo</c>).
    /// </summary>
    public static IServiceCollection AddProductPersistence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        ProductClassMap.Register();
        services.AddScoped<IProductRepository, MongoProductRepository>();
        return services;
    }
}
