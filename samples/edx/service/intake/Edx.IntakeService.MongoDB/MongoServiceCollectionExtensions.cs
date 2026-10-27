using Edx.IntakeService.Domain.StorageDomain.Consts;
using Edx.IntakeService.Domain.StorageDomain.Entities;
using Edx.IntakeService.Domain.StorageDomain.Repositories;
using Edx.IntakeService.Domain.StoredFileDomain.Consts;
using Edx.IntakeService.Domain.StoredFileDomain.Entities;
using Edx.IntakeService.MongoDB.Configurations;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Edx.IntakeService.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the intake service needs of MongoDB: the connection (<c>AddBaseMongo</c>, <c>ConnectionStrings:Mongo-IntakeService</c>, and its readiness check), the tenants of the documents, the mappings and the repositories.
    /// </summary>
    public static IServiceCollection AddIntakePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseMongo(configuration, MongoIntakeDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyMongo();

        StorageFolderClassMap.Register();
        StoredFileClassMap.Register();
        services.AddScoped<IStorageFolderRepository>(provider => new MongoStorageFolderRepository(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoIntakeDbProperties.CollectionPrefix + StorageFolderConsts.CollectionName));
        services.AddScoped<IRepository<StorageFolder, Guid>>(provider => provider.GetRequiredService<IStorageFolderRepository>());
        services.AddScoped<IRepository<StoredFile, Guid>>(provider => new MongoRepository<StoredFile, Guid>(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoIntakeDbProperties.CollectionPrefix + StoredFileConsts.CollectionName));
        return services;
    }
}
