using Edx.InvoiceService.Domain.ReceivedFileDomain.Consts;
using Edx.InvoiceService.Domain.ReceivedFileDomain.Entities;
using Edx.InvoiceService.MongoDB.Configurations;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Edx.InvoiceService.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the invoice service needs of MongoDB: the connection (<c>AddBaseMongo</c>, <c>ConnectionStrings:Mongo-InvoiceService</c>, and its readiness check), the tenants of the documents, the mapping and the repository.
    /// </summary>
    public static IServiceCollection AddInvoicePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseMongo(configuration, MongoInvoiceDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyMongo();

        ReceivedFileClassMap.Register();
        services.AddScoped<IRepository<ReceivedFile, Guid>>(provider => new MongoRepository<ReceivedFile, Guid>(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoInvoiceDbProperties.CollectionPrefix + ReceivedFileConsts.CollectionName));
        return services;
    }
}
