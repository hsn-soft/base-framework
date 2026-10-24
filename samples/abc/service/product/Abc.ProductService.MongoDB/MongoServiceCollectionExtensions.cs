using Microsoft.Extensions.DependencyInjection;

namespace Abc.ProductService.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers the repositories of the product service (they are added here as the domain has them). The <c>IMongoDatabase</c> itself is registered by the host
    /// (<c>AddBaseMongo</c>, connection string <c>ConnectionStrings:Mongo</c>).
    /// </summary>
    public static IServiceCollection AddProductPersistence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
