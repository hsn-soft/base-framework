using Edx.Integration.FirmInvoice.Domain.StorageDomain.Entities;
using Edx.Integration.FirmInvoice.Domain.StorageDomain.Repositories;
using HsnSoft.Base.MongoDB;
using MongoDB.Driver;

namespace Edx.Integration.FirmInvoice.MongoDB;

/// <summary>The folders in MongoDB: what <see cref="MongoRepository{TEntity, TKey}"/> does for any entity, and the folder that is made once when two uploads need it at the same time (the unique index of the path decides).</summary>
public sealed class MongoStorageFolderRepository(IMongoDatabase database, MongoEntityConcepts concepts, string collectionName)
    : MongoRepository<StorageFolder, Guid>(database, concepts, collectionName), IStorageFolderRepository
{
    public async Task<(StorageFolder Folder, bool Created)> GetOrAddAsync(Guid? parentId, string name, string path, FolderLevel level, CancellationToken cancellationToken = default)
    {
        StorageFolder? found = await FindAsync(folder => folder.Path == path, cancellationToken).ConfigureAwait(false);
        if (found is not null)
        {
            return (found, false);
        }

        StorageFolder folder = StorageFolder.Create(parentId, name, path, level);
        try
        {
            await AddAsync(folder, cancellationToken).ConfigureAwait(false);
            return (folder, true);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // another upload made it between the read and the insert: that one is the folder
            StorageFolder existing = await FindAsync(f => f.Path == path, cancellationToken).ConfigureAwait(false)
                                     ?? throw new InvalidOperationException($"The folder '{path}' is taken but cannot be read.");
            return (existing, false);
        }
    }
}
