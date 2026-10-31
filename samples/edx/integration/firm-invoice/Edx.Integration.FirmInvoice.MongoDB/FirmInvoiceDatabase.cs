using Edx.Integration.FirmInvoice.Domain.QueueDomain.Consts;
using Edx.Integration.FirmInvoice.Domain.QueueDomain.Entities;
using Edx.Integration.FirmInvoice.Domain.StorageDomain.Consts;
using Edx.Integration.FirmInvoice.Domain.StorageDomain.Entities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Edx.Integration.FirmInvoice.MongoDB;

public static class FirmInvoiceDatabase
{
    /// <summary>
    /// Makes the indexes: the path of a folder is unique in a tenant (what makes two uploads of the same minute share a folder), and the queue is looked up by tenant, newest first, by invoice and by folder. MongoDB creates the
    /// database and the collections when the first document is written.
    /// </summary>
    internal static async Task EnsureFirmInvoiceCollections(IServiceProvider services, CancellationToken cancellationToken)
    {
        IMongoDatabase database = services.GetRequiredService<IMongoDatabase>();

        await database.GetCollection<StorageFolder>(MongoFirmInvoiceDbProperties.CollectionPrefix + StorageFolderConsts.CollectionName).Indexes.CreateManyAsync(
            [
                new CreateIndexModel<StorageFolder>(
                    Builders<StorageFolder>.IndexKeys.Ascending(folder => folder.TenantId).Ascending(folder => folder.Path), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<StorageFolder>(Builders<StorageFolder>.IndexKeys.Ascending(folder => folder.ParentId)),
            ],
            cancellationToken).ConfigureAwait(false);

        // the queue is looked up by tenant, newest first, by the invoice and by the folder
        await database.GetCollection<QueueItem>(MongoFirmInvoiceDbProperties.CollectionPrefix + QueueItemConsts.CollectionName).Indexes.CreateManyAsync(
            [
                new CreateIndexModel<QueueItem>(Builders<QueueItem>.IndexKeys.Ascending(item => item.TenantId).Descending(item => item.CreatedAt)),
                new CreateIndexModel<QueueItem>(Builders<QueueItem>.IndexKeys.Ascending(item => item.TenantId).Ascending(item => item.InvoiceId)),
                new CreateIndexModel<QueueItem>(Builders<QueueItem>.IndexKeys.Ascending(item => item.FolderId)),
            ],
            cancellationToken).ConfigureAwait(false);
    }
}
