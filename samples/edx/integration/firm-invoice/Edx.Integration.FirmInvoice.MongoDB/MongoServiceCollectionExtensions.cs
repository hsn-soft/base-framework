using Edx.Integration.FirmInvoice.Domain.QueueDomain.Consts;
using Edx.Integration.FirmInvoice.Domain.QueueDomain.Entities;
using Edx.Integration.FirmInvoice.Domain.StorageDomain.Consts;
using Edx.Integration.FirmInvoice.Domain.StorageDomain.Entities;
using Edx.Integration.FirmInvoice.Domain.StorageDomain.Repositories;
using Edx.Integration.FirmInvoice.MongoDB.Configurations;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Edx.Integration.FirmInvoice.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the firm invoice integration needs of MongoDB: the connection (<c>AddBaseMongo</c>, <c>ConnectionStrings:Mongo-FirmInvoiceService</c>, and its readiness check), the tenants of the documents, the mappings and the repositories.
    /// </summary>
    public static IServiceCollection AddFirmInvoicePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseMongo(configuration, MongoFirmInvoiceDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyMongo();

        StorageFolderClassMap.Register();
        QueueItemClassMap.Register();
        services.AddScoped<IStorageFolderRepository>(provider => new MongoStorageFolderRepository(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoFirmInvoiceDbProperties.CollectionPrefix + StorageFolderConsts.CollectionName));
        services.AddScoped<IRepository<StorageFolder, Guid>>(provider => provider.GetRequiredService<IStorageFolderRepository>());
        services.AddScoped<IRepository<QueueItem, Guid>>(provider => new MongoRepository<QueueItem, Guid>(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoFirmInvoiceDbProperties.CollectionPrefix + QueueItemConsts.CollectionName));
        return services;
    }
}
