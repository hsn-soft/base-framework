using Abc.ProductService.Domain.ProductDomain.Repositories;
using Abc.ProductService.MongoDB.Configurations;
using Abc.ProductService.MongoDB.Repositories;
using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.ProductService.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the product service needs of MongoDB: the connection (<c>AddBaseMongo</c>, <c>ConnectionStrings:Mongo</c>, and its readiness check), the mappings and the repositories.
    /// </summary>
    public static IServiceCollection AddProductPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseMongo(configuration);

        ProductClassMap.Register();
        services.AddScoped<IProductRepository, MongoProductRepository>();
        return services;
    }
}
