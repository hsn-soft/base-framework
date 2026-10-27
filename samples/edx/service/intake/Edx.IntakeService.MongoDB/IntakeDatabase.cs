using Edx.IntakeService.Domain.StorageDomain.Consts;
using Edx.IntakeService.Domain.StorageDomain.Entities;
using Edx.IntakeService.Domain.StoredFileDomain.Consts;
using Edx.IntakeService.Domain.StoredFileDomain.Entities;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Edx.IntakeService.MongoDB;

public static class IntakeDatabase
{
    /// <summary>
    /// Makes the indexes: the path of a folder is unique in a tenant (what makes two uploads of the same minute share a folder), and the files are looked up by tenant, newest first, and by folder. MongoDB creates the
    /// database and the collections when the first document is written.
    /// </summary>
    public static Task EnsureIntakeDatabaseAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.RunStartupStepAsync("Intake collections", async cancellationToken =>
        {
            IMongoDatabase database = services.GetRequiredService<IMongoDatabase>();

            await database.GetCollection<StorageFolder>(MongoIntakeDbProperties.CollectionPrefix + StorageFolderConsts.CollectionName).Indexes.CreateManyAsync(
                [
                    new CreateIndexModel<StorageFolder>(
                        Builders<StorageFolder>.IndexKeys.Ascending(folder => folder.TenantId).Ascending(folder => folder.Path), new CreateIndexOptions { Unique = true }),
                    new CreateIndexModel<StorageFolder>(Builders<StorageFolder>.IndexKeys.Ascending(folder => folder.ParentId)),
                ],
                cancellationToken).ConfigureAwait(false);

            await database.GetCollection<StoredFile>(MongoIntakeDbProperties.CollectionPrefix + StoredFileConsts.CollectionName).Indexes.CreateManyAsync(
                [
                    new CreateIndexModel<StoredFile>(Builders<StoredFile>.IndexKeys.Ascending(file => file.TenantId).Descending(file => file.CreatedAt)),
                    new CreateIndexModel<StoredFile>(Builders<StoredFile>.IndexKeys.Ascending(file => file.FolderId)),
                ],
                cancellationToken).ConfigureAwait(false);
        });
    }
}
