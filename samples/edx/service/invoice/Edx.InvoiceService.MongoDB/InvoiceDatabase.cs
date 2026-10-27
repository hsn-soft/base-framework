using Edx.InvoiceService.Domain.ReceivedFileDomain.Consts;
using Edx.InvoiceService.Domain.ReceivedFileDomain.Entities;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Edx.InvoiceService.MongoDB;

public static class InvoiceDatabase
{
    /// <summary>Makes the indexes: the id of the file in the intake service is unique (a file is recorded once), and the records are looked up by tenant, newest first. MongoDB creates the database and the collection when the first document is written.</summary>
    public static Task EnsureInvoiceDatabaseAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.RunStartupStepAsync("ReceivedFile collection", async cancellationToken =>
        {
            IMongoCollection<ReceivedFile> files = services.GetRequiredService<IMongoDatabase>().GetCollection<ReceivedFile>(MongoInvoiceDbProperties.CollectionPrefix + ReceivedFileConsts.CollectionName);
            await files.Indexes.CreateManyAsync(
                [
                    new CreateIndexModel<ReceivedFile>(Builders<ReceivedFile>.IndexKeys.Ascending(file => file.FileId), new CreateIndexOptions { Unique = true }),
                    new CreateIndexModel<ReceivedFile>(Builders<ReceivedFile>.IndexKeys.Ascending(file => file.TenantId).Descending(file => file.ReceivedAt)),
                ],
                cancellationToken).ConfigureAwait(false);
        });
    }
}
